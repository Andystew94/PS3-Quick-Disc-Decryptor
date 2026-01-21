Option Strict On
Option Explicit On
Option Infer Off

Imports System.ComponentModel
Imports System.IO
Imports System.IO.Compression
Imports System.Runtime.InteropServices
Imports System.Runtime.Versioning
Imports System.Text
Imports System.Threading
Imports System.Windows.Media.Animation

Imports DevCase.Extensions
Imports DevCase.Win32
Imports DevCase.Win32.Enums

Imports DiscUtils
Imports DiscUtils.Iso9660

Imports Microsoft.WindowsAPICodePack.Taskbar

Imports MS.WindowsAPICodePack.Internal

Friend NotInheritable Class Form1

#Region " Private Fields "

    Friend Shared ReadOnly Settings As New ProgramSettings()

    Private Shared logFileWriter As StreamWriter

    Private isos As IEnumerable(Of FileInfo)
    Private keys As IEnumerable(Of FileInfo)
    Private isoAndKeyPairs As IDictionary(Of FileInfo, FileInfo)

    ''' <summary>
    ''' Directory where to unzip zipped isos and decryption keys.
    ''' </summary>
    Private ReadOnly tempFolderPath As String = Path.Combine(Path.GetTempPath, My.Application.Info.Title)

    ''' <summary>
    ''' PS3Dec.exe process with redirected output to TextBox.
    ''' </summary>
    Private ps3DecProcess As Process

    ''' <summary>
    ''' Open file handle that prevents PS3Dec.exe file from being deleted, moved or modified from disk.
    ''' </summary>
    Private ps3DecOpenHandle As FileStream

    ''' <summary>
    ''' Flag to request user cancellation of the asynchronous decryption procedure.
    ''' </summary>
    Private cancelRequested As Boolean

#End Region

#Region " Constructors "

    Public Sub New()
        ' This call is required by the designer.
        Me.InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        Me.Opacity = 0
    End Sub

#End Region

#Region " Event-Handlers "

    Private Sub Form1_Load(sender As Object, e As EventArgs) _
    Handles MyBase.Load

        Me.PropertyGrid_Settings.SelectedObject = Form1.Settings
        Me.Text = $"{My.Application.Info.Title} (PS3QDD) v{My.Application.Info.Version} | By ElektroStudios"
    End Sub

    Private Sub Form1_Shown(sender As Object, e As EventArgs) _
    Handles MyBase.Shown
        Me.MinimumSize = Me.Size
        Me.LoadUserSettings()
        Me.Opacity = 100

        Me.InitializeLogger()
        Me.UpdateStatus("Program has been initialized.", writeToLogFile:=True, TraceEventType.Information)
        Me.UpdateStatus("Program is ready. Press the ""Start Decryption"" button and you will see the decryption status here.", writeToLogFile:=False)
    End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) _
    Handles MyBase.FormClosing

        If e.CloseReason = CloseReason.UserClosing Then
            If Not Me.ps3DecProcess?.HasExited Then
                Dim question As DialogResult =
                    MessageBox.Show(Me, $"PS3Dec.exe is currently running and writing a decrypted disc in the output directory, if you exit this program PS3Dec.exe process will be killed.{Environment.NewLine & Environment.NewLine}Do you really want to exit?.", My.Application.Info.Title,
                                    MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation)

                If question = DialogResult.Yes Then
                    Try
                        Me.ps3DecProcess?.Kill(entireProcessTree:=False)
                        Me.UpdateStatus("PS3Dec.exe was killed on user demand (force application closure).", writeToLogFile:=True)
                    Catch ex As Exception
                    End Try
                Else
                    e.Cancel = True
                End If
            End If
        End If

        If Not e.Cancel Then
            Me.SaveUserSettings()
            Me.UpdateStatus("Program is being closed...", writeToLogFile:=True)
            Me.DeinitializeLogger()
        End If
    End Sub

    Private Sub Button_StartDecryption_Click(sender As Object, e As EventArgs) _
    Handles Button_StartDecryption.Click

        If Not Me.BackgroundWorker1.IsBusy Then
            Me.PropertyGrid_Settings.Enabled = False
            Me.Button_StartDecryption.Enabled = False
            Me.Button_Abort.Enabled = True

            Me.BackgroundWorker1.RunWorkerAsync()
        End If
    End Sub

    Private Sub Button_Abort_Click(sender As Object, e As EventArgs) _
    Handles Button_Abort.Click

        If Me.BackgroundWorker1.IsBusy Then
            Dim btn As Button = DirectCast(sender, Button)
            btn.Enabled = False
            Me.cancelRequested = True

            Me.UpdateStatus("Aborting, please wait until the current disc gets decrypted...", writeToLogFile:=False)
        End If
    End Sub


    Private Sub BackgroundWorker1_DoWork(sender As Object, e As DoWorkEventArgs) _
    Handles BackgroundWorker1.DoWork

        Me.UpdateStatus($"Starting a new decryption procedure...", writeToLogFile:=True)
        Me.ResetProgressBar()

        Me.ClearTempFiles("*")

        If Not Me.FetchISOs() OrElse
           Not Me.FetchDecryptionKeys() OrElse
           Not Me.BuildisoAndKeyPairs() OrElse
           Not Me.ValidatePS3DecExe() Then

            e.Cancel = True
            Exit Sub
        End If

        Dim totalIsoCount As Integer = Me.isoAndKeyPairs.Count
        Dim currentIsoIndex As Integer = 0
        Me.UpdateProgressBar(totalIsoCount, 0)

        If Me.isoAndKeyPairs?.Any() Then
            For Each pair As KeyValuePair(Of FileInfo, FileInfo) In Me.isoAndKeyPairs
                If Me.cancelRequested Then
                    Me.BackgroundWorker1.CancelAsync()
                    e.Cancel = True
                    Exit For
                End If

                If Not Me.BackgroundWorker1.CancellationPending Then
                    Me.ProcessDecryption(pair, currentIsoIndex, totalIsoCount)
                    Me.ClearTempFiles(Path.GetFileNameWithoutExtension(pair.Key.Name))
                    Me.UpdateProgressBar(totalIsoCount, Interlocked.Increment(currentIsoIndex))
                End If
            Next
        End If

        ' Extract ISOs if option is enabled and decryption wasn't cancelled
        If Form1.Settings.ExtractISOsAfterDecryption AndAlso Not e.Cancel Then
            Me.ExtractDecryptedISOs()
        End If
    End Sub

    Private Sub BackgroundWorker1_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) _
    Handles BackgroundWorker1.RunWorkerCompleted

        If e.Error IsNot Nothing Then
            If e.Error?.InnerException IsNot Nothing Then
                Form1.ShowMessageBoxInUIThread(Me, "Unknown error", e.Error.InnerException.Message, MessageBoxIcon.Error)
            Else
                Form1.ShowMessageBoxInUIThread(Me, "Unknown error", e.Error.Message, MessageBoxIcon.Error)
            End If
        End If

        If e.Cancelled AndAlso Me.cancelRequested Then
            Me.UpdateStatus("Decryption procedure aborted on demand.", writeToLogFile:=False)
            Form1.ShowMessageBoxInUIThread(Me, My.Application.Info.Title, "Decryption procedure aborted on demand.", MessageBoxIcon.Information)
        ElseIf e.Cancelled Then
            Me.UpdateStatus("Decryption procedure cancelled due an error.", writeToLogFile:=True, TraceEventType.Stop)
        Else
            Me.UpdateStatus("Decryption procedure completed.", writeToLogFile:=True)

            Dim completionMessage As String = "Decryption procedure completed."
            If Form1.Settings.ExtractISOsAfterDecryption Then
                completionMessage &= vbCrLf & "ISO extraction completed."
            End If

            Form1.ShowMessageBoxInUIThread(Me, My.Application.Info.Title, completionMessage, MessageBoxIcon.Information)
        End If

        Try
            Me.ps3DecOpenHandle?.Close()
            Me.ps3DecOpenHandle = Nothing
        Catch ex As Exception
            Me.UpdateStatus($"Error releasing PS3Dec.exe file handle. Error message: {ex.Message}", writeToLogFile:=True, TraceEventType.Warning)
            ' Form1.ShowMessageBoxInUIThread(Me, "Error releasing PS3Dec.exe file handle", ex.Message, MessageBoxIcon.Warning)
        End Try

        Me.isos = Nothing
        Me.keys = Nothing
        Me.isoAndKeyPairs = Nothing

        Me.cancelRequested = False

        Me.PropertyGrid_Settings.Enabled = True
        Me.Button_StartDecryption.Enabled = True
        Me.Button_Abort.Enabled = False
    End Sub

#End Region

#Region " Private Methods "

    ''' <summary>
    ''' Fetches the encrypted PS3 *.iso / *.zip files.
    ''' </summary>
    ''' <returns><see langword="True"/> if successful, <see langword="False"/> otherwise.</returns>
    Private Function FetchISOs() As Boolean

        Me.UpdateStatus("Fetching encrypted PS3 disc images...", writeToLogFile:=True)
        Try
            Me.isos = Form1.Settings.EncryptedPS3DiscsDir?.
                                     GetFiles("*.*", SearchOption.TopDirectoryOnly).
                                     Where(Function(x) x.Extension.ToLowerInvariant() = ".iso" OrElse
                                                       x.Extension.ToLowerInvariant() = ".zip")

        Catch ex As Exception
            Form1.ShowMessageBoxInUIThread(Me, "Error fetching encrypted PS3 disc images", ex.Message, MessageBoxIcon.Error)
            Return False
        End Try

        If Not Me.isos.Any() Then
            Form1.ShowMessageBoxInUIThread(Me, "Error fetching encrypted PS3 disc images", $"Can't find any ISO or ZIP file in the specified directory: '{Form1.Settings.EncryptedPS3DiscsDir}'", MessageBoxIcon.Error)
            Return False
        End If

        Return True
    End Function

    ''' <summary>
    ''' Fetches the decryption key (*.dkey or *.txt) files.
    ''' </summary>
    ''' <returns><see langword="True"/> if successful, <see langword="False"/> otherwise.</returns>
    Private Function FetchDecryptionKeys() As Boolean

        Me.UpdateStatus("Fetching decryption keys...", writeToLogFile:=True)
        Try
            Me.keys = Form1.Settings.DecryptionKeysDir?.
                                     GetFiles("*.*", SearchOption.TopDirectoryOnly).
                                     Where(Function(x) x.Extension.ToLowerInvariant() = ".dkey" OrElse
                                                       x.Extension.ToLowerInvariant() = ".txt" OrElse
                                                       x.Extension.ToLowerInvariant() = ".zip")
        Catch ex As Exception
            Form1.ShowMessageBoxInUIThread(Me, "Error fetching decryption keys", ex.Message, MessageBoxIcon.Error)
            Return False
        End Try

        If Not Me.keys?.Any() Then
            Form1.ShowMessageBoxInUIThread(Me, "Error fetching decryption keys", $"Can't find any decryption key files in the specified directory: {Form1.Settings.DecryptionKeysDir}", MessageBoxIcon.Error)
            Return False
        End If

        Return True
    End Function

    ''' <summary>
    ''' Creates a <see cref="Dictionary(Of FileInfo, Fileinfo)"/> with
    ''' <see langword="Key"/> = Encrypted ISO, and <see langword="Value"/> = Decryption key.
    ''' </summary>
    ''' <returns><see langword="True"/> if successful, <see langword="False"/> otherwise.</returns>
    Private Function BuildisoAndKeyPairs() As Boolean

        Me.UpdateStatus("Matching encrypted PS3 ISOs with decryption keys...", writeToLogFile:=True)
        Try
            Me.isoAndKeyPairs = New Dictionary(Of FileInfo, FileInfo)
            Dim unmatchedIsos As New List(Of FileInfo)
            Dim unmatchedKeys As New List(Of FileInfo)(Me.keys)

            ' Phase 1: Exact matching (existing logic)
            For Each iso As FileInfo In Me.isos
                Dim exactMatch As FileInfo =
                    (From dkey As FileInfo In unmatchedKeys
                     Where Path.GetFileNameWithoutExtension(dkey.Name).Equals(Path.GetFileNameWithoutExtension(iso.Name), StringComparison.OrdinalIgnoreCase)
                    ).SingleOrDefault()

                If exactMatch IsNot Nothing Then
                    Me.isoAndKeyPairs.Add(iso, exactMatch)
                    unmatchedKeys.Remove(exactMatch)
                Else
                    unmatchedIsos.Add(iso)
                End If
            Next iso

            ' Phase 2: Fuzzy matching for unmatched ISOs
            If unmatchedIsos.Any() AndAlso unmatchedKeys.Any() Then
                Me.UpdateStatus($"Attempting smart match for {unmatchedIsos.Count} unmatched ISO(s)...", writeToLogFile:=True)

                Dim applySkipToAll As Boolean = False
                Dim applyAutoMatchToAll As Boolean = False

                For Each iso As FileInfo In unmatchedIsos.ToList() ' ToList to allow modification during iteration
                    If Me.cancelRequested Then Exit For

                    ' Skip if user chose "Apply Skip to All"
                    If applySkipToAll Then Continue For

                    ' Find fuzzy matches
                    Dim matches As List(Of DevCase.Matching.MatchResult) =
                        DevCase.Matching.FuzzyMatcher.FindMatches(iso, unmatchedKeys, minConfidence:=0.5, maxResults:=5)

                    If Not matches.Any() Then
                        ' No suggestions available
                        Me.UpdateStatus($"No suitable matches found for: {iso.Name}", writeToLogFile:=True)
                        Continue For
                    End If

                    ' Auto-accept high confidence matches if user chose "Apply to All"
                    If applyAutoMatchToAll AndAlso matches.First().ConfidenceScore >= 0.9 Then
                        Dim topMatch As DevCase.Matching.MatchResult = matches.First()
                        Me.isoAndKeyPairs.Add(iso, topMatch.KeyFile)
                        unmatchedKeys.Remove(topMatch.KeyFile)
                        unmatchedIsos.Remove(iso)
                        Me.UpdateStatus($"Auto-matched {iso.Name} with {topMatch.KeyFile.Name} ({topMatch.GetConfidencePercentage()}%)", writeToLogFile:=True)
                        Continue For
                    End If

                    ' Show dialog to user (thread-safe invocation)
                    Dim selectedMatch As DevCase.Matching.MatchResult = Nothing
                    Dim userChoice As DevCase.Matching.MatchDialogResult = DevCase.Matching.MatchDialogResult.Skip
                    Dim applyToAll As Boolean = False

                    Me.Invoke(Sub()
                                  Using dialog As New DevCase.Matching.MatchSuggestionDialog(iso, matches)
                                      Dim dialogResult As DialogResult = dialog.ShowDialog(Me)
                                      userChoice = dialog.UserChoice
                                      applyToAll = dialog.ApplyToAll

                                      Select Case userChoice
                                          Case DevCase.Matching.MatchDialogResult.AcceptSuggestion
                                              selectedMatch = dialog.SelectedMatch

                                          Case DevCase.Matching.MatchDialogResult.ManualSelect
                                              ' Show file picker for manual selection
                                              Using openDialog As New OpenFileDialog()
                                                  openDialog.Title = $"Select decryption key for {iso.Name}"
                                                  openDialog.InitialDirectory = Form1.Settings.DecryptionKeysDir.FullName
                                                  openDialog.Filter = "Key Files|*.dkey;*.txt;*.zip|All Files|*.*"
                                                  openDialog.RestoreDirectory = True

                                                  If openDialog.ShowDialog(Me) = DialogResult.OK Then
                                                      Dim manualKey As New FileInfo(openDialog.FileName)
                                                      selectedMatch = New DevCase.Matching.MatchResult(iso, manualKey, 1.0, "Manual selection")
                                                  End If
                                              End Using
                                      End Select
                                  End Using
                              End Sub)

                    ' Process user choice
                    If selectedMatch IsNot Nothing Then
                        Me.isoAndKeyPairs.Add(iso, selectedMatch.KeyFile)
                        unmatchedKeys.Remove(selectedMatch.KeyFile)
                        unmatchedIsos.Remove(iso)
                        Me.UpdateStatus($"Matched {iso.Name} with {selectedMatch.KeyFile.Name}", writeToLogFile:=True)

                        If applyToAll AndAlso userChoice = DevCase.Matching.MatchDialogResult.AcceptSuggestion Then
                            applyAutoMatchToAll = True
                        End If
                    Else
                        ' User skipped
                        If applyToAll Then
                            applySkipToAll = True
                        End If
                    End If
                Next iso
            End If

        Catch ex As Exception
            Form1.ShowMessageBoxInUIThread(Me, "Error matching encrypted PS3 ISOs with decryption keys", ex.Message, MessageBoxIcon.Error)
            Return False
        End Try

        Dim isosCount As Integer = Me.isos.Count
        Dim diffCount As Integer = isosCount - Me.isoAndKeyPairs.Count
        If diffCount = isosCount Then
            Form1.ShowMessageBoxInUIThread(Me, "Missing decryption key matches", $"The program could not find any matching decryption keys for the available PS3 ISOs.", MessageBoxIcon.Error)
            Return False
        ElseIf diffCount <> 0 Then
            Form1.ShowMessageBoxInUIThread(Me, "Missing decryption key matches", $"The program could not find matching decryption keys for {diffCount} out of {isosCount} PS3 ISOs.{Environment.NewLine & Environment.NewLine}The program will proceed now decrypting the remaining ISOs.", MessageBoxIcon.Warning)
        End If

        Return True
    End Function

    ''' <summary>
    ''' Validates a PS3 ISO file.
    ''' </summary>
    ''' <returns><see langword="True"/> if successful, <see langword="False"/> otherwise.</returns>
    Private Function ValidatePS3Iso(ByRef refIso As FileInfo, percentage As Integer, currentIsoIndex As Integer, totalIsoCount As Integer) As Boolean

        refIso = Me.UnzipFileIfNecessary(refIso, percentage, currentIsoIndex, totalIsoCount, "PS3 ISO")
        If refIso Is Nothing Then
            Return False
        End If

        Me.UpdateStatus($"{percentage}% ({currentIsoIndex}/{totalIsoCount}) | {refIso.Name} | Validating encrypted PS3 ISO...", writeToLogFile:=True)
        Try
            Using isoStream As FileStream = File.OpenRead(refIso.FullName),
                  cd As New CDReader(isoStream, joliet:=False)

                Dim isExpectedClusterSize As Boolean = cd.ClusterSize = 2048

                Dim existsPS3GAMEDir As Boolean =
                    cd.Root.GetDirectories("PS3_GAME", SearchOption.TopDirectoryOnly).Any()

                If Not isExpectedClusterSize OrElse
                   Not existsPS3GAMEDir Then
                    Form1.ShowMessageBoxInUIThread(Me, "Error validating encrypted PS3 ISO", $"The ISO file is not a PS3 disc image: {refIso.FullName}", MessageBoxIcon.Error)
                    Return False
                End If
            End Using

        Catch ex As Exception
            Form1.ShowMessageBoxInUIThread(Me, $"Error validating encrypted PS3 PS3 ISO.{Environment.NewLine & Environment.NewLine}File: {refIso.FullName}{Environment.NewLine & Environment.NewLine}Error message: {ex.Message}", ex.Message, MessageBoxIcon.Error)
            Return False
        End Try

        Return True
    End Function

    ''' <summary>
    ''' Validates the PS3Dec.exe file.
    ''' </summary>
    ''' <returns><see langword="True"/> if successful, <see langword="False"/> otherwise.</returns>
    Private Function ValidatePS3DecExe() As Boolean

        Me.UpdateStatus("Acquiring PS3Dec.exe file handle...", writeToLogFile:=True)
        If Not Form1.Settings.PS3DecExeFile.Exists Then
            Form1.ShowMessageBoxInUIThread(Me, "Error acquiring PS3Dec.exe file handle", $"PS3Dec.exe was not found at: {Form1.Settings.PS3DecExeFile.FullName}", MessageBoxIcon.Error)
            Return False
        End If

        Try
            Me.ps3DecOpenHandle = Form1.Settings.PS3DecExeFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read)
        Catch ex As Exception
            Form1.ShowMessageBoxInUIThread(Me, "Error acquiring PS3Dec.exe file handle", ex.Message, MessageBoxIcon.Error)
            Return False
        End Try

        Return True
    End Function

    ''' <summary>
    ''' Processes the decryption of an encrypted PS3 ISO.
    ''' </summary>
    Private Sub ProcessDecryption(pair As KeyValuePair(Of FileInfo, FileInfo), currentIsoIndex As Integer, totalIsoCount As Integer)

        Dim percentage As Integer = CInt(currentIsoIndex / totalIsoCount * 100)

        Dim dkeyString As String = Nothing
        If Not Me.ReadDecryptionKey(pair.Value, dkeyString, percentage, currentIsoIndex, totalIsoCount) Then
            Exit Sub
        End If

        Dim refIso As FileInfo = pair.Key
        Dim sizeString As String = Me.FormatFileSize(refIso.Length, StrFormatByteSizeFlags.RoundToNearest)

        If Not Me.ValidatePS3Iso(refIso, percentage, currentIsoIndex, totalIsoCount) Then
            Exit Sub
        End If

        Me.UpdateStatus($"{percentage}% ({currentIsoIndex}/{totalIsoCount}) | {refIso.Name} | Writing decrypted PS3 ISO ({sizeString}) to output directory...", writeToLogFile:=True)
        If Not Me.EnsureOutputDirectoryExists() Then
            Exit Sub
        End If

        Dim drive As DriveInfo = Form1.Settings.DecryptionKeysDir.GetDriveInfo()
        Dim requiredSizeString As String = Me.FormatFileSize(refIso.Length, StrFormatByteSizeFlags.RoundToNearest)
        If Not Me.DriveHasFreeSpace(drive, refIso.Length) Then
            Form1.ShowMessageBoxInUIThread(Me, $"Error writing decrypted PS3 ISO.", $"Drive {drive} requires {requiredSizeString} of free space to write the file.", MessageBoxIcon.Error)
            Exit Sub
        End If

        Me.ExecutePS3Dec(pair, refIso, dkeyString, currentIsoIndex, totalIsoCount)

    End Sub

    ''' <summary>
    ''' Reads and validates the decryption key from a file.
    ''' </summary>
    ''' <returns><see langword="True"/> if successful, <see langword="False"/> otherwise.</returns>
    Private Function ReadDecryptionKey(keyFile As FileInfo, ByRef refKeyString As String, percentage As Integer, currentIsoIndex As Integer, totalIsoCount As Integer) As Boolean

        keyFile = Me.UnzipFileIfNecessary(keyFile, percentage, currentIsoIndex, totalIsoCount, "Decryption key")
        If keyFile Is Nothing Then
            Return False
        End If

        Me.UpdateStatus($"{percentage}% ({currentIsoIndex}/{totalIsoCount}) | {keyFile.Name} | Parsing decryption key file content...", writeToLogFile:=True)
        Try
            Dim dkeyString As String = File.ReadAllText(keyFile.FullName, Encoding.Default).Trim()
            If dkeyString.Length <> 32 Then
                Form1.ShowMessageBoxInUIThread(Me, "Error parsing decryption key file content", $"File: {keyFile.FullName}{Environment.NewLine & Environment.NewLine}Error message: Decryption key has an invalid length of {dkeyString.Length} (it must be 32 character length).", MessageBoxIcon.Error)
                Return False
            End If

#If (NET7_0_OR_GREATER) Then
            Dim isHex As Boolean = dkeyString.All(Function(c As Char) Char.IsAsciiHexDigit(c)) AndAlso (dkeyString.Length Mod 2) = 0 ' is even.
#Else
            Dim isHexString As Boolean = StringExtensions.IsHexadecimal(dkeyString)
#End If
            If Not isHexString Then
                Form1.ShowMessageBoxInUIThread(Me, "Error parsing decryption key file content", $"File: {keyFile.FullName}{Environment.NewLine & Environment.NewLine}Error message: Decryption key has not a valid hexadecimal format.", MessageBoxIcon.Error)
                Return False
            End If

            refKeyString = dkeyString

        Catch ex As Exception
            Form1.ShowMessageBoxInUIThread(Me, "Error reading decryption key file", $"File: {keyFile.FullName}{Environment.NewLine & Environment.NewLine}Error message: {ex.Message}", MessageBoxIcon.Error)
            Return False

        End Try

        Return True
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <returns><see langword="True"/> if successful, <see langword="False"/> otherwise.</returns>
    Private Function EnsureOutputDirectoryExists() As Boolean

        Form1.Settings.OutputDir.Refresh()
        If Not Form1.Settings.OutputDir.Exists Then
            Try
                Form1.Settings.OutputDir.Create()
            Catch ex As Exception
                Form1.ShowMessageBoxInUIThread(Me, "Error creating output directory", ex.Message, MessageBoxIcon.Error)
                Return False
            End Try
        End If
        Return True
    End Function

    Private Sub ExecutePS3Dec(pair As KeyValuePair(Of FileInfo, FileInfo), isoFile As FileInfo, dkeyString As String, currentIsoIndex As Integer, totalIsoCount As Integer)

        ' Run PS3Dec.exe and capture output to TextBox
        Me.ps3DecProcess = New Process()
        With Me.ps3DecProcess
            .StartInfo.FileName = Form1.Settings.PS3DecExeFile.FullName
            .StartInfo.Arguments = $"d key ""{dkeyString}"" ""{isoFile.FullName}"" ""{Form1.Settings.OutputDir.FullName}\{isoFile.Name}"""
            .StartInfo.UseShellExecute = False
            .StartInfo.CreateNoWindow = True
            .StartInfo.RedirectStandardOutput = True
            .StartInfo.RedirectStandardError = True
            .StartInfo.StandardOutputEncoding = Encoding.UTF8
            .StartInfo.StandardErrorEncoding = Encoding.UTF8
        End With

        ' Clear TextBox before starting
        Me.TextBox_PS3Dec_Output.Invoke(Sub()
                                            Me.TextBox_PS3Dec_Output.Clear()
                                            Me.TextBox_PS3Dec_Output.AppendText($"Starting PS3Dec.exe...{Environment.NewLine}")
                                        End Sub)

        Try
            Me.ps3DecProcess.Start()

            ' Read output asynchronously and update TextBox
            Dim outputBuilder As New StringBuilder()

            AddHandler Me.ps3DecProcess.OutputDataReceived, Sub(sender As Object, e As DataReceivedEventArgs)
                If e.Data IsNot Nothing Then
                    outputBuilder.AppendLine(e.Data)
                    Me.TextBox_PS3Dec_Output.Invoke(Sub()
                        Me.TextBox_PS3Dec_Output.AppendText(e.Data & Environment.NewLine)
                        Me.TextBox_PS3Dec_Output.SelectionStart = Me.TextBox_PS3Dec_Output.Text.Length
                        Me.TextBox_PS3Dec_Output.ScrollToCaret()
                    End Sub)
                End If
            End Sub

            AddHandler Me.ps3DecProcess.ErrorDataReceived, Sub(sender As Object, e As DataReceivedEventArgs)
                If e.Data IsNot Nothing Then
                    outputBuilder.AppendLine($"ERROR: {e.Data}")
                    Me.TextBox_PS3Dec_Output.Invoke(Sub()
                        Me.TextBox_PS3Dec_Output.AppendText($"ERROR: {e.Data}{Environment.NewLine}")
                        Me.TextBox_PS3Dec_Output.SelectionStart = Me.TextBox_PS3Dec_Output.Text.Length
                        Me.TextBox_PS3Dec_Output.ScrollToCaret()
                    End Sub)
                End If
            End Sub

            Me.ps3DecProcess.BeginOutputReadLine()
            Me.ps3DecProcess.BeginErrorReadLine()

            Me.ps3DecProcess.WaitForExit()

        Catch ex As Exception
            Form1.ShowMessageBoxInUIThread(Me, "Error executing PS3Dec.exe", ex.Message, MessageBoxIcon.Error)
            Exit Sub
        End Try

        If Me.ps3DecProcess.ExitCode = 0 Then
            Dim percentage As Integer = CInt(currentIsoIndex / totalIsoCount * 100)
            Me.UpdateStatus($"{percentage}% ({currentIsoIndex}/{totalIsoCount}) | {isoFile.Name} | Decryption completed.", writeToLogFile:=True)
            Me.CanDeleteEncryptedISO(pair.Key)
            Me.CanDeleteDecryptionKey(pair.Value)
        Else
            Me.UpdateStatus($"PS3Dec.exe failed to decrypt, with process exit code: {Me.ps3DecProcess.ExitCode}", writeToLogFile:=True)
        End If
    End Sub

    Private Sub CanDeleteEncryptedISO(iso As FileInfo)
        If Form1.Settings.DeleteDecryptedISOs Then
            Me.UpdateStatus($"Deleting encrypted PS3 ISO: {iso.Name}...", writeToLogFile:=True)
            Try
                iso.Delete()
            Catch ex As Exception
                Me.UpdateStatus($"Error deleting encrypted PS3 ISO. Error message: {ex.Message}", writeToLogFile:=True, TraceEventType.Warning)
                ' Form1.ShowMessageBoxInUIThread(Me, "Error deleting encrypted PS3 ISO", ex.Message, MessageBoxIcon.Warning)
            End Try
        End If
    End Sub

    Private Sub CanDeleteDecryptionKey(key As FileInfo)
        If Form1.Settings.DeleteKeysAfterUse Then
            Me.UpdateStatus($"Deleting decryption key: {key.Name}...", writeToLogFile:=True)
            Try
                key.Delete()
            Catch ex As Exception
                Me.UpdateStatus($"Error deleting decryption key. Error message: {ex.Message}", writeToLogFile:=True, TraceEventType.Warning)
                ' Form1.ShowMessageBoxInUIThread(Me, "Error deleting decryption key", ex.Message, MessageBoxIcon.Warning)
            End Try
        End If
    End Sub

    Private Sub ClearTempFiles(fileNamePattern As String)
        Dim dir As New DirectoryInfo(Me.tempFolderPath)
        If Not dir.Exists Then
            Return
        End If

        Me.UpdateStatus("Clearing temp files...", writeToLogFile:=False)
        For Each file As FileInfo In dir.GetFiles($"{fileNamePattern}.*", SearchOption.TopDirectoryOnly)
            Try
                file.Delete()
            Catch ex As Exception
                ' Ignore.
            End Try
        Next file
    End Sub

    ''' <summary>
    ''' If the source file is a zip archive, unzips it to the temporary folder (<see cref="Form1.tempFolderPath"/>).
    ''' </summary>
    ''' <returns>
    ''' If the source file is a zip archive, returns a <see cref="FileInfo"/> pointing to the unzipped file.
    ''' Otherwise, returns the source <see cref="FileInfo"/>.
    ''' </returns>
    Private Function UnzipFileIfNecessary(file As FileInfo, percentage As Integer, currentIsoIndex As Integer, totalIsoCount As Integer, additionalStatusInfoString As String) As FileInfo

        If Not file.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase) Then
            Return file
        End If

        Me.UpdateStatus($"{percentage}% ({currentIsoIndex}/{totalIsoCount}) | {file.Name} | Validating zip archive ({additionalStatusInfoString})...", writeToLogFile:=True)
        If Not Me.ValidateZipArchive(file) Then
            Return Nothing
        End If

        Me.UpdateStatus($"{percentage}% ({currentIsoIndex}/{totalIsoCount}) | {file.Name} | Unzipping file ({additionalStatusInfoString})...", writeToLogFile:=True)
        Try
            Dim drive As DriveInfo = New DirectoryInfo(tempFolderPath).GetDriveInfo()
            Const safetyExtraSize As Long = CLng(1024 ^ 2) * 300 ' 300 MB
            Dim requiredSizeString As String = Me.FormatFileSize(file.Length + safetyExtraSize, StrFormatByteSizeFlags.RoundToNearest)
            If Not Me.DriveHasFreeSpace(drive, file.Length) Then
                Form1.ShowMessageBoxInUIThread(Me, $"Error extracting zip archive.", $"Drive {drive} requires {requiredSizeString} of free space to extract the zip archive.", MessageBoxIcon.Error)
                Return Nothing
            End If

            Using archive As ZipArchive = ZipFile.OpenRead(file.FullName)

                Dim entry As ZipArchiveEntry = archive.Entries.Single()
                Dim destinationPath As String = Path.Combine(Me.tempFolderPath, entry.Name)
                Dim unzippedFile As New FileInfo(destinationPath)

                If Not Directory.Exists(Me.tempFolderPath) Then
                    Directory.CreateDirectory(Me.tempFolderPath)
                End If

                entry.ExtractToFile(unzippedFile.FullName, overwrite:=True)
                unzippedFile.Refresh()
                Return unzippedFile
            End Using

        Catch ex As Exception
            Form1.ShowMessageBoxInUIThread(Me, $"Error extracting zip archive.", ex.Message, MessageBoxIcon.Error)

        End Try

        Return Nothing

    End Function

    ''' <summary>
    ''' Validates whether the specified file is a zip archive, 
    ''' and whether the zip archive only contains a single file.
    ''' </summary>
    ''' <returns><see langword="True"/> if successful, <see langword="False"/> otherwise.</returns>
    Private Function ValidateZipArchive(file As FileInfo) As Boolean

        Try
            Using archive As ZipArchive = ZipFile.OpenRead(file.FullName)
                Select Case archive.Entries.Count
                    Case 1
                        Return True
                    Case 0
                        Form1.ShowMessageBoxInUIThread(Me, $"Error validating zip archive.", $"The zip archive is empty: {file.FullName}", MessageBoxIcon.Error)
                        Return False
                    Case Else
                        Form1.ShowMessageBoxInUIThread(Me, $"Error validating zip archive.", $"The zip archive contains more than one file: {file.FullName}", MessageBoxIcon.Error)
                        Return False
                End Select
            End Using

        Catch ex As InvalidDataException
            Return False

        Catch ex As Exception
            Form1.ShowMessageBoxInUIThread(Me, $"Error validating zip archive.", ex.Message, MessageBoxIcon.Error)
            Return False

        End Try

    End Function

    Private Sub UpdateStatus(statusText As String, writeToLogFile As Boolean, Optional eventType As TraceEventType = TraceEventType.Information)
        Me.ToolStripStatusLabel1.Text = statusText
        If writeToLogFile Then
            Form1.WriteLogEntry(eventType, statusText)
        End If
    End Sub

    Private Sub InitializeLogger()
        If Not Form1.Settings.LogEnabled Then
            Return
        End If

        Form1.logFileWriter = New StreamWriter(Form1.Settings.LogFile.FullName, append:=Form1.Settings.LogAppendMode, encoding:=Encoding.UTF8, bufferSize:=1024) With {.AutoFlush = True}
        Form1.logFileWriter.WriteLine(String.Format("          Log Date: {0}          ", Date.Now.Date.ToShortDateString()))
        Form1.logFileWriter.WriteLine("=========================================")
        Form1.logFileWriter.WriteLine()
    End Sub

    Private Shared Sub WriteLogEntry(eventType As TraceEventType, message As String)
        If Not Form1.Settings.LogEnabled OrElse Form1.logFileWriter Is Nothing Then
            Return
        End If

        Dim localDate As String = Date.Now.Date.ToShortDateString()
        Dim localTime As String = Date.Now.ToLongTimeString()
        Const entryFormat As String = "[{1}] | {2,-11} | {3}" ' {0}=Date, {1}=Time, {2}=Event, {3}=Message.

        Form1.logFileWriter.WriteLine(String.Format(entryFormat, localDate, localTime, eventType.ToString(), message))
    End Sub

    Private Sub DeinitializeLogger()
        If Not Form1.Settings.LogEnabled OrElse Form1.logFileWriter Is Nothing Then
            Return
        End If

        Try
            Form1.logFileWriter.WriteLine("End of log session.")
            Form1.logFileWriter.WriteLine()
            Form1.logFileWriter.Close()
        Catch ex As Exception
            ' Ignore.
        End Try
    End Sub

    Private Sub ResetProgressBar()
        Me.UpdateProgressBar(1, 0)
        TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal)
        TaskbarManager.Instance.SetProgressValue(0, 0)
    End Sub

    Private Sub UpdateProgressBar(maxValue As Integer, currentValue As Integer)
        Me.ProgressBar_Decryption.Invoke(Sub()
                                             Me.ProgressBar_Decryption.Maximum = maxValue
                                             Me.ProgressBar_Decryption.Value = currentValue
                                         End Sub)
        TaskbarManager.Instance.SetProgressValue(currentValue, maxValue)
    End Sub

    Friend Shared Sub ShowMessageBoxInUIThread(f As Form, title As String, message As String, icon As MessageBoxIcon)
        f.Invoke(Sub() MessageBox.Show(f, message, title, MessageBoxButtons.OK, icon))

        Select Case icon
            Case MessageBoxIcon.Error
                Form1.WriteLogEntry(TraceEventType.Critical, message)

            Case MessageBoxIcon.Warning
                Form1.WriteLogEntry(TraceEventType.Warning, message)

            Case Else
                Form1.WriteLogEntry(TraceEventType.Information, message)
        End Select
    End Sub

    Private Sub SaveUserSettings()
        Try
            If Form1.Settings.SaveSettingsOnExit Then
                Me.UpdateStatus($"Saving user settings...", writeToLogFile:=False)
                My.Settings.EncryptedPS3DiscsDir = Form1.Settings.EncryptedPS3DiscsDir.FullName
                My.Settings.DecryptionKeysDir = Form1.Settings.DecryptionKeysDir.FullName
                My.Settings.PS3DecExePath = Form1.Settings.PS3DecExeFile.FullName
                My.Settings.OutputDir = Form1.Settings.OutputDir.FullName
                My.Settings.DeleteDecryptedISOs = Form1.Settings.DeleteDecryptedISOs
                My.Settings.DeleteKeysAfterUse = Form1.Settings.DeleteKeysAfterUse
                My.Settings.ExtractISOsAfterDecryption = Form1.Settings.ExtractISOsAfterDecryption
                My.Settings.RememberSizeAndPosition = Form1.Settings.RememberSizeAndPosition
                If My.Settings.RememberSizeAndPosition AndAlso Me.WindowState <> FormWindowState.Minimized Then
                    My.Settings.WindowPosition = Me.Location
                    My.Settings.WindowSize = Me.Size
                End If
                My.Settings.LogEnabled = Form1.Settings.LogEnabled
                My.Settings.LogAppendMode = Form1.Settings.LogAppendMode
                My.Settings.SaveSettingsOnExit = Form1.Settings.SaveSettingsOnExit
                My.Settings.Save()
            Else
                My.Settings.Reset()
            End If
        Catch ex As Exception
            Form1.ShowMessageBoxInUIThread(Me, My.Application.Info.Title, $"Error saving user settings: {ex.Message}", MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub LoadUserSettings()

        Try
            If My.Settings.SaveSettingsOnExit Then
                Me.UpdateStatus($"Loading user settings...", writeToLogFile:=False)
                Dim dirPath As String = My.Application.Info.DirectoryPath
                Form1.Settings.EncryptedPS3DiscsDir = New DirectoryInfo(My.Settings.EncryptedPS3DiscsDir.Replace(dirPath, "."))
                Form1.Settings.DecryptionKeysDir = New DirectoryInfo(My.Settings.DecryptionKeysDir.Replace(dirPath, "."))
                Form1.Settings.PS3DecExeFile = New FileInfo(My.Settings.PS3DecExePath.Replace(dirPath, "."))
                Form1.Settings.OutputDir = New DirectoryInfo(My.Settings.OutputDir.Replace(dirPath, "."))
                Form1.Settings.DeleteDecryptedISOs = My.Settings.DeleteDecryptedISOs
                Form1.Settings.DeleteKeysAfterUse = My.Settings.DeleteKeysAfterUse
                Form1.Settings.ExtractISOsAfterDecryption = My.Settings.ExtractISOsAfterDecryption
                Form1.Settings.RememberSizeAndPosition = My.Settings.RememberSizeAndPosition
                If Form1.Settings.RememberSizeAndPosition Then
                    Me.Location = My.Settings.WindowPosition
                    Me.Size = My.Settings.WindowSize
                End If
                Form1.Settings.LogEnabled = My.Settings.LogEnabled
                Form1.Settings.LogAppendMode = My.Settings.LogAppendMode
                Form1.Settings.SaveSettingsOnExit = My.Settings.SaveSettingsOnExit
            End If
        Catch ex As Exception
            Form1.ShowMessageBoxInUIThread(Me, My.Application.Info.Title, $"Error loading user settings: {ex.Message}", MessageBoxIcon.Error)
        End Try
    End Sub


    Public Function DriveHasFreeSpace(drive As DriveInfo, requiredSpaceInBytes As Long) As Boolean

        If drive.IsReady Then
            Dim freeSpace As Long = drive.AvailableFreeSpace
            Return freeSpace > requiredSpaceInBytes
        Else
            Return False
        End If

    End Function

    Private Function FormatFileSize(bytes As Long, flags As StrFormatByteSizeFlags) As String

        Dim buffer As New StringBuilder(8, 16)
        Dim result As Integer = NativeMethods.StrFormatByteSizeEx(CULng(bytes), flags, buffer, 16)
        If result <> 0 Then ' HResult.S_OK
            Marshal.ThrowExceptionForHR(result)
        End If

        Return buffer.ToString()

    End Function

    ''' <summary>
    ''' Extracts all decrypted ISO files in the output directory.
    ''' </summary>
    Private Sub ExtractDecryptedISOs()
        Try
            Me.UpdateStatus("Starting ISO extraction...", writeToLogFile:=True)

            Dim outputDir As DirectoryInfo = Form1.Settings.OutputDir
            If Not outputDir.Exists Then
                Me.UpdateStatus("Output directory does not exist. Skipping extraction.", writeToLogFile:=True, TraceEventType.Warning)
                Return
            End If

            ' Get all ISO files in the output directory
            Dim isoFiles As FileInfo() = outputDir.GetFiles("*.iso", SearchOption.TopDirectoryOnly)
            If isoFiles.Length = 0 Then
                Me.UpdateStatus("No ISO files found for extraction.", writeToLogFile:=True)
                Return
            End If

            Me.UpdateStatus($"Found {isoFiles.Length} ISO file(s) to extract.", writeToLogFile:=True)

            Dim successCount As Integer = 0
            Dim failCount As Integer = 0

            For Each isoFile As FileInfo In isoFiles
                Try
                    Me.UpdateStatus($"Extracting: {isoFile.Name}...", writeToLogFile:=True)

                    ' Create extraction directory with same name as ISO
                    Dim extractDirName As String = Path.GetFileNameWithoutExtension(isoFile.Name)
                    Dim extractDirPath As String = Path.Combine(outputDir.FullName, extractDirName)
                    Dim extractDir As New DirectoryInfo(extractDirPath)

                    If extractDir.Exists Then
                        Me.UpdateStatus($"Extraction directory already exists, skipping: {extractDirName}", writeToLogFile:=True, TraceEventType.Warning)
                        Continue For
                    End If

                    extractDir.Create()

                    ' Choose extraction method based on settings
                    Select Case Form1.Settings.IsoExtractionMethod
                        Case IsoExtractionMethod.SevenZip
                            Me.ExtractISOUsing7Zip(isoFile, extractDirPath)
                        Case IsoExtractionMethod.DiscUtils
                            Me.ExtractISOUsingDiscUtils(isoFile, extractDirPath)
                    End Select

                    successCount += 1
                    Me.UpdateStatus($"Successfully extracted: {isoFile.Name}", writeToLogFile:=True)

                Catch ex As Exception
                    failCount += 1
                    Me.UpdateStatus($"Error extracting {isoFile.Name}: {ex.Message}", writeToLogFile:=True, TraceEventType.Error)
                End Try
            Next

            Me.UpdateStatus($"ISO extraction completed. Success: {successCount}, Failed: {failCount}", writeToLogFile:=True)

        Catch ex As Exception
            Me.UpdateStatus($"Error during ISO extraction: {ex.Message}", writeToLogFile:=True, TraceEventType.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Extracts an ISO file using DiscUtils library.
    ''' </summary>
    Private Sub ExtractISOUsingDiscUtils(isoFile As FileInfo, extractDirPath As String)
        Using isoStream As FileStream = isoFile.OpenRead(),
              cd As New CDReader(isoStream, joliet:=True)

            Dim fileCount As Integer = 0
            Me.ExtractDirectoryDiscUtils(cd, cd.Root.FullName, extractDirPath, fileCount)
            Me.UpdateStatus($"  Total: {fileCount} files extracted", writeToLogFile:=False)
        End Using
    End Sub

    ''' <summary>
    ''' Recursively extracts a directory from an ISO image using DiscUtils.
    ''' </summary>
    Private Sub ExtractDirectoryDiscUtils(cd As CDReader, sourceDir As String, targetDir As String, ByRef fileCount As Integer)
        ' Extract all files in current directory
        For Each fileName As String In cd.GetFiles(sourceDir)
            Try
                ' Get filename and strip ISO9660 version suffix (;1, ;2, etc.)
                Dim cleanFileName As String = Path.GetFileName(fileName)
                Dim semicolonIndex As Integer = cleanFileName.LastIndexOf(";"c)
                If semicolonIndex > 0 Then
                    cleanFileName = cleanFileName.Substring(0, semicolonIndex)
                End If

                Dim targetPath As String = Path.Combine(targetDir, cleanFileName)

                ' Update status every 10 files to reduce UI overhead
                fileCount += 1
                If fileCount Mod 10 = 0 Then
                    Me.UpdateStatus($"  Extracted {fileCount} files...", writeToLogFile:=False)
                End If

                Using sourceStream As Stream = cd.OpenFile(fileName, FileMode.Open),
                      targetStream As FileStream = File.Create(targetPath)
                    ' Use larger buffer for better performance
                    Dim buffer(81920 - 1) As Byte ' 80KB buffer
                    Dim bytesRead As Integer
                    Do
                        bytesRead = sourceStream.Read(buffer, 0, buffer.Length)
                        If bytesRead > 0 Then
                            targetStream.Write(buffer, 0, bytesRead)
                        End If
                    Loop While bytesRead > 0
                End Using

            Catch ex As Exception
                Me.UpdateStatus($"  Warning: Failed to extract {fileName}: {ex.Message}", writeToLogFile:=True, TraceEventType.Warning)
            End Try
        Next

        ' Recursively extract subdirectories
        For Each dirName As String In cd.GetDirectories(sourceDir)
            Try
                Dim dirBaseName As String = Path.GetFileName(dirName.TrimEnd("\"c, "/"c))
                Dim targetSubDir As String = Path.Combine(targetDir, dirBaseName)
                Directory.CreateDirectory(targetSubDir)
                Me.ExtractDirectoryDiscUtils(cd, dirName, targetSubDir, fileCount)
            Catch ex As Exception
                Me.UpdateStatus($"  Warning: Failed to extract directory {dirName}: {ex.Message}", writeToLogFile:=True, TraceEventType.Warning)
            End Try
        Next
    End Sub

    ''' <summary>
    ''' Extracts an ISO file using 7-Zip command line (fastest).
    ''' </summary>
    Private Sub ExtractISOUsing7Zip(isoFile As FileInfo, extractDirPath As String)
        ' Check if 7z.exe exists
        If Not Form1.Settings.SevenZipExePath.Exists Then
            Throw New FileNotFoundException($"7z.exe not found at: {Form1.Settings.SevenZipExePath.FullName}")
        End If

        ' Build 7-Zip command: 7z x "iso_path" -o"output_path" -y
        Dim arguments As String = $"x ""{isoFile.FullName}"" -o""{extractDirPath}"" -y"

        Me.UpdateStatus($"  Running: 7z {arguments}", writeToLogFile:=True)

        ' Create process to run 7-Zip
        Dim psi As New ProcessStartInfo() With {
            .FileName = Form1.Settings.SevenZipExePath.FullName,
            .Arguments = arguments,
            .UseShellExecute = False,
            .RedirectStandardOutput = True,
            .RedirectStandardError = True,
            .CreateNoWindow = True,
            .WorkingDirectory = extractDirPath
        }

        Using proc As Process = Process.Start(psi)
            ' Read output for progress tracking
            Dim output As String = proc.StandardOutput.ReadToEnd()
            Dim errorOutput As String = proc.StandardError.ReadToEnd()

            proc.WaitForExit()

            If proc.ExitCode <> 0 Then
                Throw New Exception($"7-Zip extraction failed with exit code {proc.ExitCode}. Error: {errorOutput}")
            End If

            ' Count extracted files
            Dim filesExtracted As Integer = Directory.GetFiles(extractDirPath, "*", SearchOption.AllDirectories).Length
            Me.UpdateStatus($"  Total: {filesExtracted} files extracted", writeToLogFile:=False)
        End Using
    End Sub

#End Region

End Class


Module FileSystemInfoExtensions
    <System.Runtime.CompilerServices.Extension>
    Public Function GetDriveInfo(fsi As IO.FileSystemInfo) As DriveInfo

        If fsi Is Nothing Then
            Throw New ArgumentNullException(paramName:=NameOf(fsi))
        End If

        Dim driveName As String = Path.GetPathRoot(fsi.FullName)
        Return New DriveInfo(driveName)
    End Function
End Module