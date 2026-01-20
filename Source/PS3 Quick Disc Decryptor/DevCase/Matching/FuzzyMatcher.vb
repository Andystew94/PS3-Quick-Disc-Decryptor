Option Strict On
Option Explicit On
Option Infer Off

Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions

Namespace DevCase.Matching

    ''' <summary>
    ''' Provides fuzzy matching functionality for ISO files and decryption keys.
    ''' </summary>
    Friend NotInheritable Class FuzzyMatcher

        Private Sub New()
            ' Prevent instantiation
        End Sub

        ''' <summary>
        ''' Finds the best matching decryption keys for a given ISO file.
        ''' </summary>
        ''' <param name="isoFile">The ISO file to find matches for.</param>
        ''' <param name="candidateKeys">The collection of candidate decryption key files.</param>
        ''' <param name="minConfidence">Minimum confidence score to include in results (0.0 to 1.0). Default is 0.5.</param>
        ''' <param name="maxResults">Maximum number of results to return. Default is 5.</param>
        ''' <returns>A list of <see cref="MatchResult"/> objects sorted by confidence (highest first).</returns>
        Public Shared Function FindMatches(isoFile As FileInfo,
                                          candidateKeys As IEnumerable(Of FileInfo),
                                          Optional minConfidence As Double = 0.5,
                                          Optional maxResults As Integer = 5) As List(Of MatchResult)

            Dim isoName As String = Path.GetFileNameWithoutExtension(isoFile.Name)
            Dim results As New List(Of MatchResult)

            For Each keyFile As FileInfo In candidateKeys
                Dim keyName As String = Path.GetFileNameWithoutExtension(keyFile.Name)

                ' Calculate composite similarity score
                Dim score As Double = CalculateSimilarityScore(isoName, keyName)

                If score >= minConfidence Then
                    Dim details As String = BuildMatchDetails(isoName, keyName, score)
                    results.Add(New MatchResult(isoFile, keyFile, score, details))
                End If
            Next

            ' Sort by confidence (highest first) and take top N
            Return results.OrderByDescending(Function(r) r.ConfidenceScore).Take(maxResults).ToList()
        End Function

        ''' <summary>
        ''' Calculates a composite similarity score between two filenames using multiple algorithms.
        ''' </summary>
        Private Shared Function CalculateSimilarityScore(name1 As String, name2 As String) As Double
            Dim levenshteinScore As Double = CalculateLevenshteinSimilarity(name1, name2)
            Dim tokenScore As Double = CalculateTokenMatchScore(name1, name2)
            Dim gameIdScore As Double = CalculateGameIDScore(name1, name2)
            Dim metadataScore As Double = CalculateMetadataScore(name1, name2)

            ' Weighted combination: Token matching has highest weight (40%)
            Return (levenshteinScore * 0.3) + (tokenScore * 0.4) + (gameIdScore * 0.2) + (metadataScore * 0.1)
        End Function

        ''' <summary>
        ''' Calculates Levenshtein distance normalized to a 0.0-1.0 similarity score.
        ''' </summary>
        Private Shared Function CalculateLevenshteinSimilarity(s1 As String, s2 As String) As Double
            s1 = s1.ToLowerInvariant()
            s2 = s2.ToLowerInvariant()

            Dim distance As Integer = ComputeLevenshteinDistance(s1, s2)
            Dim maxLength As Integer = Math.Max(s1.Length, s2.Length)

            If maxLength = 0 Then Return 1.0

            Return 1.0 - (CDbl(distance) / CDbl(maxLength))
        End Function

        ''' <summary>
        ''' Computes the Levenshtein distance between two strings.
        ''' </summary>
        Private Shared Function ComputeLevenshteinDistance(s1 As String, s2 As String) As Integer
            Dim len1 As Integer = s1.Length
            Dim len2 As Integer = s2.Length
            Dim matrix(len1, len2) As Integer

            ' Initialize first row and column
            For i As Integer = 0 To len1
                matrix(i, 0) = i
            Next
            For j As Integer = 0 To len2
                matrix(0, j) = j
            Next

            ' Fill the matrix
            For i As Integer = 1 To len1
                For j As Integer = 1 To len2
                    Dim cost As Integer = If(s1(i - 1) = s2(j - 1), 0, 1)
                    matrix(i, j) = Math.Min(Math.Min(
                        matrix(i - 1, j) + 1,            ' deletion
                        matrix(i, j - 1) + 1),           ' insertion
                        matrix(i - 1, j - 1) + cost)     ' substitution
                Next
            Next

            Return matrix(len1, len2)
        End Function

        ''' <summary>
        ''' Calculates a similarity score based on matching tokens (words) in the filenames.
        ''' </summary>
        Private Shared Function CalculateTokenMatchScore(name1 As String, name2 As String) As Double
            Dim tokens1 As String() = TokenizeFilename(name1)
            Dim tokens2 As String() = TokenizeFilename(name2)

            If tokens1.Length = 0 OrElse tokens2.Length = 0 Then Return 0.0

            ' Count matching tokens (case-insensitive)
            Dim matchCount As Integer = 0
            For Each token1 As String In tokens1
                If tokens2.Any(Function(t As String) t.Equals(token1, StringComparison.OrdinalIgnoreCase)) Then
                    matchCount += 1
                End If
            Next

            ' Normalize by average token count
            Dim avgTokenCount As Double = CDbl(tokens1.Length + tokens2.Length) / 2.0
            Return CDbl(matchCount) / avgTokenCount
        End Function

        ''' <summary>
        ''' Splits a filename into tokens by common separators.
        ''' </summary>
        Private Shared Function TokenizeFilename(filename As String) As String()
            ' Split by common separators: underscore, space, hyphen, dot, brackets
            Dim separators As Char() = {"_"c, " "c, "-"c, "."c, "["c, "]"c, "("c, ")"c}
            Return filename.Split(separators, StringSplitOptions.RemoveEmptyEntries)
        End Function

        ''' <summary>
        ''' Calculates a score based on matching PS3 Game IDs (e.g., BLUS-12345).
        ''' </summary>
        Private Shared Function CalculateGameIDScore(name1 As String, name2 As String) As Double
            Dim gameId1 As String = ExtractGameID(name1)
            Dim gameId2 As String = ExtractGameID(name2)

            If String.IsNullOrEmpty(gameId1) OrElse String.IsNullOrEmpty(gameId2) Then
                Return 0.0 ' No game ID found in one or both filenames
            End If

            Return If(gameId1.Equals(gameId2, StringComparison.OrdinalIgnoreCase), 1.0, 0.0)
        End Function

        ''' <summary>
        ''' Extracts a PS3 Game ID from a filename using regex (e.g., BLUS-12345).
        ''' </summary>
        Private Shared Function ExtractGameID(filename As String) As String
            Dim pattern As String = "[A-Z]{4}-?\d{5}"
            Dim match As Match = Regex.Match(filename, pattern, RegexOptions.IgnoreCase)
            Return If(match.Success, match.Value.ToUpperInvariant(), String.Empty)
        End Function

        ''' <summary>
        ''' Calculates a score based on matching metadata like region codes and language codes.
        ''' </summary>
        Private Shared Function CalculateMetadataScore(name1 As String, name2 As String) As Double
            Dim score As Double = 0.0
            Dim matchCount As Integer = 0

            ' Check region codes
            Dim regions As String() = {"USA", "Europe", "Japan", "Asia", "World", "PAL", "NTSC"}
            For Each regionCode As String In regions
                Dim inName1 As Boolean = name1.IndexOf(regionCode, StringComparison.OrdinalIgnoreCase) >= 0
                Dim inName2 As Boolean = name2.IndexOf(regionCode, StringComparison.OrdinalIgnoreCase) >= 0
                If inName1 AndAlso inName2 Then
                    score += 1.0
                    matchCount += 1
                End If
            Next

            ' Check language codes
            Dim languages As String() = {"EN", "FR", "DE", "ES", "IT", "JA", "PT", "RU"}
            For Each langCode As String In languages
                Dim inName1 As Boolean = name1.IndexOf(langCode, StringComparison.OrdinalIgnoreCase) >= 0
                Dim inName2 As Boolean = name2.IndexOf(langCode, StringComparison.OrdinalIgnoreCase) >= 0
                If inName1 AndAlso inName2 Then
                    score += 1.0
                    matchCount += 1
                End If
            Next

            ' Normalize
            Return If(matchCount > 0, score / CDbl(matchCount), 0.0)
        End Function

        ''' <summary>
        ''' Builds a human-readable explanation of why two filenames match.
        ''' </summary>
        Private Shared Function BuildMatchDetails(name1 As String, name2 As String, score As Double) As String
            Dim details As New StringBuilder()

            ' Game ID match
            Dim gameId1 As String = ExtractGameID(name1)
            Dim gameId2 As String = ExtractGameID(name2)
            If Not String.IsNullOrEmpty(gameId1) AndAlso gameId1.Equals(gameId2, StringComparison.OrdinalIgnoreCase) Then
                details.AppendLine($"Game ID: {gameId1}")
            End If

            ' Token overlap
            Dim tokens1 As String() = TokenizeFilename(name1)
            Dim tokens2 As String() = TokenizeFilename(name2)
            Dim commonTokens As List(Of String) = tokens1.Intersect(tokens2, StringComparer.OrdinalIgnoreCase).ToList()
            If commonTokens.Any() AndAlso commonTokens.Count <= 5 Then
                details.Append($"Common: {String.Join(", ", commonTokens)}")
            ElseIf commonTokens.Count > 5 Then
                details.Append($"Common: {commonTokens.Count} matching words")
            End If

            Return details.ToString().TrimEnd()
        End Function

    End Class

End Namespace
