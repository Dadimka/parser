using System.ComponentModel;
using ToCCourseWork.Entity;

namespace ToCCourseWork.Service
{
    public class Parser
    {
        private readonly List<Token> tokens;
        private readonly List<Error> errors;
        public List<string> ExecutionStack { get; set; }

        public Parser(List<Token> tokens, List<Error> errors)
        {
            this.tokens = tokens;
            this.errors = errors;
            ExecutionStack = new List<string>();
        }

        public List<Error> Parse()
        {

            List<Error> errors = [.. this.errors];
            foreach (List<Token> line in SplitTokensIntoLines(tokens))
            {
                RecursiveParser recursiveParser = new RecursiveParser(line);
                errors.AddRange(recursiveParser.Parse());
                ExecutionStack.AddRange(recursiveParser.ExecutionStack);
            }


            return errors
                .OrderBy(e => e.Line)
                .ThenBy(e => e.Column)
                .ToList();
        }


        private List<List<Token>> SplitTokensIntoLines(List<Token> tokens)
        {
            List<List<Token>> lines = new List<List<Token>>();
            List<Token> currentLine = new List<Token>();

            foreach (Token token in tokens)
            {
                currentLine.Add(token);

                if (token.Type == TokenType.WHITESPACE)
                {
                    lines.Add(currentLine);
                    currentLine = new List<Token>();
                }
            }

            if (currentLine.Count > 0)
            {
                lines.Add(currentLine);
            }

            return lines;
        }
    }


    public class RecursiveParser
    {
        private readonly List<Token> tokens;
        private int OpenBracket = 0;
        public List<string> ExecutionStack { get; set; }

        public RecursiveParser(List<Token> tokens)
        {
            this.tokens = tokens;
            ExecutionStack = new List<string>();
        }

        public List<Error> Parse()
        {
            int currentPosition = 0;
            List<Error> errors = new List<Error>();
            return Expression(currentPosition, errors).Item1;
        }

        private (List<Error>, int) Expression(int currentPosition, List<Error> errors)
        {
            ExecutionStack.Add("E");
            if (IsAtEnd(currentPosition))
            {
                return (errors, currentPosition);
            }


            if (Match(currentPosition, TokenType.WHITESPACE, errors))
                return Expression(currentPosition + 1, errors);

            (errors, currentPosition) = T(currentPosition, errors);
            (errors, currentPosition) = A(currentPosition, errors);
            if(OpenBracket == 0 && Match(currentPosition, TokenType.CLOSE_BRACKET, errors))
            {
                AddError(currentPosition, TokenType.CLOSE_BRACKET, ErrorType.DELETE, errors);
            }
            return (errors, currentPosition);
        }

        private (List<Error>, int) T(int currentPosition, List<Error> errors)
        {
            ExecutionStack.Add("T");
            if (IsAtEnd(currentPosition))
            {
                return (errors, currentPosition);
            }
            (errors, currentPosition) = O(currentPosition, errors);
            return B(currentPosition, errors);
        }

        private (List<Error>, int) A(int currentPosition, List<Error> errors)
        {
            ExecutionStack.Add("A");
            if (IsAtEnd(currentPosition))
            {
                return (errors, currentPosition);
            }

            if (Match(currentPosition, TokenType.WHITESPACE, errors))
                return Expression(currentPosition + 1, errors);

            if (Match(currentPosition, TokenType.NUMBER, errors) ||
                Match(currentPosition, TokenType.IDENTIFIER, errors) || 
                Match(currentPosition, TokenType.OPEN_BRACKET, errors))
            {
                AddError(currentPosition, TokenType.PLUS_OPERATION, ErrorType.PUSH, errors);
                (errors, currentPosition) = T(currentPosition, errors);
                return A(currentPosition, errors);

            } else if (Match(currentPosition, TokenType.PLUS_OPERATION, errors))
            {
                (errors, currentPosition) = T(currentPosition + 1, errors);
                return A(currentPosition, errors);
            } else if (OpenBracket == 0 && Match(currentPosition, TokenType.CLOSE_BRACKET,errors) &&
                (Match(currentPosition + 1, TokenType.PLUS_OPERATION, errors) || Match(currentPosition + 1, TokenType.CLOSE_BRACKET, errors)))
            {
                AddError(currentPosition, TokenType.PLUS_OPERATION, ErrorType.DELETE, errors);
                return A(currentPosition+1,errors);
            } else
            {
                return (errors, currentPosition);
            }


        }

        private (List<Error>, int) O(int currentPosition, List<Error> errors)
        {
            ExecutionStack.Add("O");
            if (IsAtEnd(currentPosition))
            {
                return (errors, currentPosition);
            }


            if (Match(currentPosition, TokenType.WHITESPACE, errors))
                return Expression(currentPosition + 1, errors);

            if (Match(currentPosition, TokenType.IDENTIFIER, errors) || Match(currentPosition, TokenType.NUMBER, errors))
            {
                return (errors, currentPosition + 1);
            } else if (Match(currentPosition, TokenType.OPEN_BRACKET, errors))
            {
                OpenBracket++;
                (errors, currentPosition) = Expression(currentPosition + 1, errors);
                OpenBracket--;
                return CloseBracket(currentPosition, errors);
            }
            else
            {
                if (OpenBracket == 0 && Match(currentPosition, TokenType.CLOSE_BRACKET, errors))
                {
                    AddError(currentPosition, TokenType.IDENTIFIER, ErrorType.REPLACE, errors);
                    return (errors, currentPosition + 1);
                } else
                {
                    AddError(currentPosition, TokenType.IDENTIFIER, ErrorType.PUSH, errors);
                }
            }
            return (errors, currentPosition);
        }

        private (List<Error>, int) B(int currentPosition, List<Error> errors)
        {
            ExecutionStack.Add("B");
            if (IsAtEnd(currentPosition))
            {
                return (errors, currentPosition);
            }

            if (Match(currentPosition, TokenType.WHITESPACE, errors))
                return Expression(currentPosition + 1, errors);

            if (Match(currentPosition, TokenType.NUMBER, errors) ||
                Match(currentPosition, TokenType.IDENTIFIER, errors) ||
                Match(currentPosition, TokenType.OPEN_BRACKET, errors))
            {
                AddError(currentPosition, TokenType.MULTIPLICATION_OPERATION, ErrorType.PUSH, errors);
                (errors, currentPosition) = O(currentPosition, errors);
                return B(currentPosition, errors);

            }
            else if (Match(currentPosition, TokenType.MULTIPLICATION_OPERATION, errors))
            {
                (errors, currentPosition) = O(currentPosition + 1, errors);
                return B(currentPosition, errors);
            }
            else if (OpenBracket == 0 && Match(currentPosition, TokenType.CLOSE_BRACKET, errors) && 
                (Match(currentPosition+1, TokenType.MULTIPLICATION_OPERATION, errors) || Match(currentPosition + 1, TokenType.CLOSE_BRACKET, errors)))
            {
                AddError(currentPosition, TokenType.PLUS_OPERATION, ErrorType.DELETE, errors);
                return B(currentPosition + 1 ,errors);
            }
            else
            {
                return (errors, currentPosition);
            }
        }

        private (List<Error>, int) CloseBracket(int currentPosition, List<Error> errors)
        {
            ExecutionStack.Add("CLOSE_BRACKET");
            if (IsAtEnd(currentPosition))
            {
                AddError(currentPosition - 1, TokenType.CLOSE_BRACKET, ErrorType.PUSH, errors);
                return (errors, currentPosition);
            }


            if (Match(currentPosition, TokenType.WHITESPACE, errors))
                return Expression(currentPosition + 1, errors);

            if (!Match(currentPosition, TokenType.CLOSE_BRACKET, errors))
            {
                AddError(currentPosition, TokenType.CLOSE_BRACKET, ErrorType.PUSH, errors);
            }
            return (errors, currentPosition + 1);
        }


        private bool Match(int currentPosition, TokenType expectedTokentype, List<Error> errors)
        {
            return Check(currentPosition, expectedTokentype, errors);
        }

        private bool Check(int currentPosition, TokenType expectedTokenType, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                return false;
            }
            else
            {
                return GetToken(currentPosition).Type == expectedTokenType;
            }
        }

        private List<Error> CreateErrorList(
                int currentPosition,
                TokenType expectedTokenType,
                ErrorType errorType,
                List<Error> errorEntities
        )
        {
            List<Error> errors = new List<Error>(errorEntities);
            AddError(currentPosition, expectedTokenType, errorType, errors);
            return errors;
        }

        private List<Error> GetMinErrorList(List<Error> e1, List<Error> e2, List<Error> e3)
        {
            if (e1.Count <= e2.Count && e1.Count <= e3.Count)
            {
                return e1;
            }
            else if (e2.Count <= e1.Count && e2.Count <= e3.Count)
            {
                return e2;
            }
            else
            {
                return e3;
            }
        }

        private (List<Error>, int) GetMinErrorList(
            (List<Error> Errors, int Count) tuple1,
            (List<Error> Errors, int Count) tuple2,
            (List<Error> Errors, int Count) tuple3)
        {
            if (tuple1.Errors.Count <= tuple2.Errors.Count && tuple1.Errors.Count <= tuple3.Errors.Count)
                return tuple1;

            if (tuple2.Errors.Count <= tuple1.Errors.Count && tuple2.Errors.Count <= tuple3.Errors.Count)
                return tuple2;

            return tuple3;
        }


        private Token GetToken(int currentPosition)
        {
            return tokens[currentPosition];
        }


        private bool IsAtEnd(int currentPosition)
        {
            return currentPosition >= tokens.Count;
        }

        private bool IsContainToken(TokenType tokenType, int pos)
        {
            for(int i = pos; i<tokens.Count; i++)
            {
                if (tokens[i].Type == tokenType)
                    return true;
            }
            return false;
        }

        private void AddError(int currentPosition, TokenType expectedTokenType, ErrorType errorType, List<Error> errors)
        {
           

            errors.Add(new Error(
                CreateErrorMessage(currentPosition, expectedTokenType, errorType),
                GetToken(currentPosition).Line,
                GetToken(currentPosition).EndColumn
            ));
        }

        private string CreateErrorMessage(int currentPosition, TokenType expectedTokenType, ErrorType errorType)
        {
            var actualToken = GetToken(currentPosition);

            return errorType switch
            {
                ErrorType.DELETE or ErrorType.DELETE_END =>
                    $"{errorType.GetDescription()}: '{actualToken.Value}'",

                ErrorType.PUSH =>
                    $"{errorType.GetDescription()}: '{GetTokenTypeDescription(expectedTokenType)}'",

                ErrorType.REPLACE =>
                    $"Ожидалось: '{GetTokenTypeDescription(expectedTokenType)}' Фактически: '{actualToken.Value}'",

                _ => string.Empty
            };
        }

        private string GetTokenTypeDescription(TokenType type)
        {
            return type switch
            {
                TokenType.NUMBER => "число",
                TokenType.IDENTIFIER => "идентификатор",
                TokenType.OPEN_BRACKET => "(",
                TokenType.CLOSE_BRACKET => ")",
                TokenType.PLUS_OPERATION => "+ или -",
                TokenType.MULTIPLICATION_OPERATION => "* или /",
                TokenType.INVALID => "невалидный токен",
                TokenType.WHITESPACE => "пробел",
                _ => type.ToString()
            };
        }
    }
}
