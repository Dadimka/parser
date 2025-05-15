using ToCCourseWork.Entity;
using System.Text.RegularExpressions;


namespace ToCCourseWork.Service
{
    public class Lexer
    {
        private static readonly Dictionary<TokenType, string> tokenPatterns = new()
    {
        { TokenType.LAMBDA,       @"^\blambda\b" },
        { TokenType.NUMBER,       @"^\d+" },
        { TokenType.IDENTIFIER,  @"^[a-zA-Z][a-zA-Z0-9]*"},
        { TokenType.EQUAL,  @"^="},
        { TokenType.PLUS_OPERATION,  @"^[+-]"},
        { TokenType.MULTIPLICATION_OPERATION,  @"^[*/]"},
        { TokenType.OPEN_BRACKET,  @"^\(" },
        { TokenType.CLOSE_BRACKET, @"^\)" },
        { TokenType.COLON, @"^:" },
        { TokenType.COMMA, @"^," },
        { TokenType.END, @"^;" },
        { TokenType.WHITESPACE,   @"^\s+" }, 
        { TokenType.INVALID,      @"^[^a-zA-Z0-9\s=\(\)+\-*=:,;/]+" },
    };

        public List<Error> Errors { get; } = new();

        public List<Token> GetTokens(string input)
        {
            List<Token> tokens = AnalyzeString(input);
            for (int i = 0; i < tokens.Count; i++)
            {
                Token currentToken = tokens[i];
                if (currentToken.Type == TokenType.INVALID)
                {
                    Errors.Add(
                        new Error(
                            $"Невалидная последовательность символов: {currentToken.Value}",
                            currentToken.Line,
                            currentToken.StartColumn
                        )
                    );
                    tokens.RemoveAt(i);
                    if (i > 0 && i < tokens.Count)
                    {
                        Token nextToken = tokens[i];
                        Token previousToken = tokens[i - 1];
                        if (previousToken.Type != TokenType.WHITESPACE && nextToken.Type != TokenType.WHITESPACE)
                        {
                            tokens.RemoveAt(i);
                            tokens.RemoveAt(i - 1);
                            int j = i - 1;
                            foreach (Token t in AnalyzeToken(input, previousToken, nextToken))
                            {
                                tokens.Insert(j, t);
                                j++;
                            }
                            i--;
                        }
                    }
                }
            }
            return tokens;
        }


        private List<Token> AnalyzeString(string input)
        {
            List<Token> tokens = new List<Token>();
            int position = 0;
            string remainingText = input.Substring(position);
            int lineNumber = 1;
            while (position < input.Length)
            {
                remainingText = input.Substring(position);
                foreach (var pattern in tokenPatterns)
                {
                    var regex = new Regex(pattern.Value);
                    var match = regex.Match(remainingText);
                    if (match.Success)
                    {
                        string value = match.Value;
                        int columnStart = position;
                        int columnEnd = position + value.Length;
                        if (pattern.Key != TokenType.WHITESPACE)
                        {
                            tokens.Add(
                                new Token(
                                    pattern.Key,
                                    input.Substring(columnStart, value.Length),
                                    lineNumber,
                                    columnStart,
                                    columnEnd
                                )
                            );
                        }
                        if (pattern.Key == TokenType.WHITESPACE)
                        {
                            int newlines = value.Count(c => c == '\n');
                            lineNumber += newlines;
                        }
                        position += match.Length;
                        break;
                    }
                }
            }
            return tokens;
        }

        private List<Token> AnalyzeToken(string input, Token previousToken, Token nextToken)
        {
            string analyzeString = input.Substring(previousToken.StartColumn, previousToken.EndColumn - previousToken.StartColumn) + 
                input.Substring(nextToken.StartColumn, nextToken.EndColumn - nextToken.StartColumn);
            List<Token> analyzeToken = AnalyzeString(analyzeString);
            if (analyzeString.Length > 2) 
            {
                string newAnalyzeString = "";
                foreach(Token t in analyzeToken)
                {
                    if (t.Type != TokenType.INVALID)
                    {
                        newAnalyzeString += analyzeString.Substring(t.StartColumn, t.EndColumn - t.StartColumn);
                    }
                }
                analyzeToken = AnalyzeString(newAnalyzeString);
            }
            if (analyzeToken.Count == 2)
            {
                return new List<Token> {previousToken, nextToken};
            } else
            {
                return new List<Token> { new Token(
                    analyzeToken[0].Type,
                    input.Substring(previousToken.StartColumn, nextToken.EndColumn - previousToken.StartColumn),
                    previousToken.Line,
                    previousToken.StartColumn,
                    nextToken.EndColumn
                )};
            }
        }
        
    }

}
