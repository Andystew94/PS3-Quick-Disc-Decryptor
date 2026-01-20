Option Strict On
Option Explicit On
Option Infer Off

Imports System.Drawing
Imports System.IO

Namespace DevCase.Matching

    ''' <summary>
    ''' Represents a potential match between an ISO file and a decryption key file.
    ''' </summary>
    Friend NotInheritable Class MatchResult

        ''' <summary>
        ''' Gets the ISO file.
        ''' </summary>
        Public ReadOnly Property IsoFile As FileInfo

        ''' <summary>
        ''' Gets the decryption key file that potentially matches the ISO.
        ''' </summary>
        Public ReadOnly Property KeyFile As FileInfo

        ''' <summary>
        ''' Gets the confidence score of this match (0.0 to 1.0).
        ''' </summary>
        Public ReadOnly Property ConfidenceScore As Double

        ''' <summary>
        ''' Gets a human-readable explanation of why this match was suggested.
        ''' </summary>
        Public ReadOnly Property MatchDetails As String

        ''' <summary>
        ''' Initializes a new instance of the <see cref="MatchResult"/> class.
        ''' </summary>
        Public Sub New(iso As FileInfo, key As FileInfo, score As Double, details As String)
            Me.IsoFile = iso
            Me.KeyFile = key
            Me.ConfidenceScore = score
            Me.MatchDetails = details
        End Sub

        ''' <summary>
        ''' Gets the confidence score formatted as a percentage (0-100).
        ''' </summary>
        Public Function GetConfidencePercentage() As Integer
            Return CInt(Me.ConfidenceScore * 100)
        End Function

        ''' <summary>
        ''' Gets the color associated with the confidence score for visual display.
        ''' </summary>
        Public Function GetConfidenceColor() As Color
            If Me.ConfidenceScore >= 0.9 Then
                Return Color.Green
            ElseIf Me.ConfidenceScore >= 0.7 Then
                Return Color.YellowGreen
            ElseIf Me.ConfidenceScore >= 0.5 Then
                Return Color.Orange
            Else
                Return Color.Gray
            End If
        End Function

    End Class

End Namespace
