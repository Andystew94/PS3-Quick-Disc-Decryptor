Option Strict On
Option Explicit On
Option Infer Off

Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms

Namespace DevCase.Matching

    ''' <summary>
    ''' Dialog for presenting fuzzy match suggestions to the user.
    ''' </summary>
    Friend NotInheritable Class MatchSuggestionDialog
        Inherits Form

        ' UI Controls
        Private WithEvents Label_ISOName As Label
        Private WithEvents Label_Instructions As Label
        Private WithEvents ListBox_Matches As ListBox
        Private WithEvents Button_Accept As Button
        Private WithEvents Button_Skip As Button
        Private WithEvents Button_Manual As Button
        Private WithEvents CheckBox_ApplyToAll As CheckBox
        Private TableLayoutPanel_Main As TableLayoutPanel
        Private Panel_Buttons As Panel

        ' Public properties
        Public Property UserChoice As MatchDialogResult = MatchDialogResult.Skip
        Public Property SelectedMatch As MatchResult = Nothing
        Public Property ApplyToAll As Boolean = False

        ''' <summary>
        ''' Initializes a new instance of the <see cref="MatchSuggestionDialog"/> class.
        ''' </summary>
        Public Sub New(isoFile As FileInfo, matches As List(Of MatchResult))
            Me.InitializeComponent()

            ' Set ISO name
            Me.Label_ISOName.Text = $"ISO File: {isoFile.Name}"

            ' Populate matches in ListBox
            For Each match As MatchResult In matches
                Me.ListBox_Matches.Items.Add(match)
            Next

            ' Pre-select the best match (first item)
            If matches.Count > 0 Then
                Me.ListBox_Matches.SelectedIndex = 0
            End If
        End Sub

        ''' <summary>
        ''' Initializes the UI components.
        ''' </summary>
        Private Sub InitializeComponent()
            Me.Label_ISOName = New Label()
            Me.Label_Instructions = New Label()
            Me.ListBox_Matches = New ListBox()
            Me.Button_Accept = New Button()
            Me.Button_Skip = New Button()
            Me.Button_Manual = New Button()
            Me.CheckBox_ApplyToAll = New CheckBox()
            Me.TableLayoutPanel_Main = New TableLayoutPanel()
            Me.Panel_Buttons = New Panel()

            Me.SuspendLayout()
            Me.TableLayoutPanel_Main.SuspendLayout()
            Me.Panel_Buttons.SuspendLayout()

            ' Form properties
            Me.Text = "Match Suggestion Required"
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.ClientSize = New Size(650, 450)
            Me.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point)

            ' Label_ISOName
            Me.Label_ISOName.Dock = DockStyle.Fill
            Me.Label_ISOName.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold, GraphicsUnit.Point)
            Me.Label_ISOName.Location = New Point(3, 0)
            Me.Label_ISOName.Name = "Label_ISOName"
            Me.Label_ISOName.Size = New Size(644, 30)
            Me.Label_ISOName.TabIndex = 0
            Me.Label_ISOName.Text = "ISO File: "
            Me.Label_ISOName.TextAlign = ContentAlignment.MiddleLeft

            ' Label_Instructions
            Me.Label_Instructions.Dock = DockStyle.Fill
            Me.Label_Instructions.Location = New Point(3, 30)
            Me.Label_Instructions.Name = "Label_Instructions"
            Me.Label_Instructions.Size = New Size(644, 40)
            Me.Label_Instructions.TabIndex = 1
            Me.Label_Instructions.Text = "Exact match not found. Please review suggested matches below and select one, or choose to skip this file:"
            Me.Label_Instructions.TextAlign = ContentAlignment.MiddleLeft

            ' ListBox_Matches
            Me.ListBox_Matches.Dock = DockStyle.Fill
            Me.ListBox_Matches.DrawMode = DrawMode.OwnerDrawFixed
            Me.ListBox_Matches.ItemHeight = 60
            Me.ListBox_Matches.Location = New Point(3, 70)
            Me.ListBox_Matches.Name = "ListBox_Matches"
            Me.ListBox_Matches.Size = New Size(644, 280)
            Me.ListBox_Matches.TabIndex = 2

            ' Button_Accept
            Me.Button_Accept.Location = New Point(10, 10)
            Me.Button_Accept.Name = "Button_Accept"
            Me.Button_Accept.Size = New Size(150, 35)
            Me.Button_Accept.TabIndex = 0
            Me.Button_Accept.Text = "Accept Match"
            Me.Button_Accept.UseVisualStyleBackColor = True

            ' Button_Skip
            Me.Button_Skip.Location = New Point(170, 10)
            Me.Button_Skip.Name = "Button_Skip"
            Me.Button_Skip.Size = New Size(150, 35)
            Me.Button_Skip.TabIndex = 1
            Me.Button_Skip.Text = "Skip This ISO"
            Me.Button_Skip.UseVisualStyleBackColor = True

            ' Button_Manual
            Me.Button_Manual.Location = New Point(330, 10)
            Me.Button_Manual.Name = "Button_Manual"
            Me.Button_Manual.Size = New Size(150, 35)
            Me.Button_Manual.TabIndex = 2
            Me.Button_Manual.Text = "Manual Select..."
            Me.Button_Manual.UseVisualStyleBackColor = True

            ' CheckBox_ApplyToAll
            Me.CheckBox_ApplyToAll.Anchor = AnchorStyles.Right Or AnchorStyles.Top
            Me.CheckBox_ApplyToAll.AutoSize = True
            Me.CheckBox_ApplyToAll.Location = New Point(490, 18)
            Me.CheckBox_ApplyToAll.Name = "CheckBox_ApplyToAll"
            Me.CheckBox_ApplyToAll.Size = New Size(150, 19)
            Me.CheckBox_ApplyToAll.TabIndex = 3
            Me.CheckBox_ApplyToAll.Text = "Apply choice to all remaining"
            Me.CheckBox_ApplyToAll.UseVisualStyleBackColor = True

            ' Panel_Buttons
            Me.Panel_Buttons.Controls.Add(Me.Button_Accept)
            Me.Panel_Buttons.Controls.Add(Me.Button_Skip)
            Me.Panel_Buttons.Controls.Add(Me.Button_Manual)
            Me.Panel_Buttons.Controls.Add(Me.CheckBox_ApplyToAll)
            Me.Panel_Buttons.Dock = DockStyle.Fill
            Me.Panel_Buttons.Location = New Point(3, 350)
            Me.Panel_Buttons.Name = "Panel_Buttons"
            Me.Panel_Buttons.Size = New Size(644, 97)
            Me.Panel_Buttons.TabIndex = 3

            ' TableLayoutPanel_Main
            Me.TableLayoutPanel_Main.ColumnCount = 1
            Me.TableLayoutPanel_Main.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
            Me.TableLayoutPanel_Main.Controls.Add(Me.Label_ISOName, 0, 0)
            Me.TableLayoutPanel_Main.Controls.Add(Me.Label_Instructions, 0, 1)
            Me.TableLayoutPanel_Main.Controls.Add(Me.ListBox_Matches, 0, 2)
            Me.TableLayoutPanel_Main.Controls.Add(Me.Panel_Buttons, 0, 3)
            Me.TableLayoutPanel_Main.Dock = DockStyle.Fill
            Me.TableLayoutPanel_Main.Location = New Point(0, 0)
            Me.TableLayoutPanel_Main.Name = "TableLayoutPanel_Main"
            Me.TableLayoutPanel_Main.RowCount = 4
            Me.TableLayoutPanel_Main.RowStyles.Add(New RowStyle(SizeType.Absolute, 30.0F))
            Me.TableLayoutPanel_Main.RowStyles.Add(New RowStyle(SizeType.Absolute, 40.0F))
            Me.TableLayoutPanel_Main.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
            Me.TableLayoutPanel_Main.RowStyles.Add(New RowStyle(SizeType.Absolute, 100.0F))
            Me.TableLayoutPanel_Main.Size = New Size(650, 450)
            Me.TableLayoutPanel_Main.TabIndex = 0

            ' Form
            Me.Controls.Add(Me.TableLayoutPanel_Main)

            Me.TableLayoutPanel_Main.ResumeLayout(False)
            Me.Panel_Buttons.ResumeLayout(False)
            Me.Panel_Buttons.PerformLayout()
            Me.ResumeLayout(False)
        End Sub

        ''' <summary>
        ''' Custom drawing for ListBox items to show confidence bars and details.
        ''' </summary>
        Private Sub ListBox_Matches_DrawItem(sender As Object, e As DrawItemEventArgs) Handles ListBox_Matches.DrawItem
            If e.Index < 0 Then Return

            e.DrawBackground()

            Dim match As MatchResult = DirectCast(Me.ListBox_Matches.Items(e.Index), MatchResult)
            Dim confidenceColor As Color = match.GetConfidenceColor()
            Dim confidencePercent As Integer = match.GetConfidencePercentage()

            ' Draw confidence bar as background
            Dim barWidth As Integer = CInt((e.Bounds.Width - 10) * match.ConfidenceScore)
            Using brush As New SolidBrush(Color.FromArgb(50, confidenceColor))
                e.Graphics.FillRectangle(brush, e.Bounds.X + 5, e.Bounds.Y + 5, barWidth, e.Bounds.Height - 10)
            End Using

            ' Draw filename with confidence percentage
            Dim mainText As String = $"{match.KeyFile.Name}  ({confidencePercent}%)"
            Using textBrush As New SolidBrush(e.ForeColor)
                e.Graphics.DrawString(mainText, e.Font, textBrush, e.Bounds.X + 10, e.Bounds.Y + 8)
            End Using

            ' Draw match details (smaller font, italic)
            If Not String.IsNullOrEmpty(match.MatchDetails) Then
                Using detailFont As New Font(e.Font.FontFamily, 7.5F, FontStyle.Italic)
                    Using detailBrush As New SolidBrush(Color.Gray)
                        e.Graphics.DrawString(match.MatchDetails, detailFont, detailBrush,
                                            e.Bounds.X + 10, e.Bounds.Y + 30)
                    End Using
                End Using
            End If

            e.DrawFocusRectangle()
        End Sub

        ''' <summary>
        ''' Handles the Accept button click event.
        ''' </summary>
        Private Sub Button_Accept_Click(sender As Object, e As EventArgs) Handles Button_Accept.Click
            If Me.ListBox_Matches.SelectedIndex >= 0 Then
                Me.SelectedMatch = DirectCast(Me.ListBox_Matches.SelectedItem, MatchResult)
                Me.UserChoice = MatchDialogResult.AcceptSuggestion
                Me.ApplyToAll = Me.CheckBox_ApplyToAll.Checked
                Me.DialogResult = DialogResult.OK
                Me.Close()
            Else
                MessageBox.Show(Me, "Please select a match from the list.", "No Selection",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        End Sub

        ''' <summary>
        ''' Handles the Skip button click event.
        ''' </summary>
        Private Sub Button_Skip_Click(sender As Object, e As EventArgs) Handles Button_Skip.Click
            Me.UserChoice = MatchDialogResult.Skip
            Me.ApplyToAll = Me.CheckBox_ApplyToAll.Checked
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
        End Sub

        ''' <summary>
        ''' Handles the Manual Select button click event.
        ''' </summary>
        Private Sub Button_Manual_Click(sender As Object, e As EventArgs) Handles Button_Manual.Click
            Me.UserChoice = MatchDialogResult.ManualSelect
            Me.DialogResult = DialogResult.Retry
            Me.Close()
        End Sub

    End Class

    ''' <summary>
    ''' Represents the user's choice from the match suggestion dialog.
    ''' </summary>
    Friend Enum MatchDialogResult
        ''' <summary>
        ''' User chose to skip this ISO file.
        ''' </summary>
        Skip

        ''' <summary>
        ''' User accepted the suggested match.
        ''' </summary>
        AcceptSuggestion

        ''' <summary>
        ''' User chose to manually select a decryption key.
        ''' </summary>
        ManualSelect
    End Enum

End Namespace
