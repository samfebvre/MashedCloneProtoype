using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PluginMaster
{
    public class PatternMachine
    {

        #region STATES AND TOKENS

        public enum PatternState
        {
            START,
            INDEX,
            OPENING_PARENTHESIS,
            CLOSING_PARENTHESIS,
            COMMA,
            ASTERISK,
            MULTIPLIER,
            ELLIPSIS,
            END,
        }

        public class Token
        {

            #region Statics and Constants

            public static Token START               = new Token( PatternState.START );
            public static Token OPENING_PARENTHESIS = new Token( PatternState.OPENING_PARENTHESIS );
            public static Token CLOSING_PARENTHESIS = new Token( PatternState.CLOSING_PARENTHESIS );
            public static Token COMMA               = new Token( PatternState.COMMA );
            public static Token ASTERISK            = new Token( PatternState.ASTERISK );
            public static Token ELLIPSIS            = new Token( PatternState.ELLIPSIS );
            public static Token END                 = new Token( PatternState.END );

            #endregion

            #region Public Fields

            public readonly PatternState state = PatternState.START;

            #endregion

            #region Protected Constructors

            protected Token( PatternState state )
            {
                this.state = state;
            }

            #endregion

        }

        public class IntToken : Token
        {

            #region Public Fields

            public readonly int value = -1;

            #endregion

            #region Public Constructors

            public IntToken( int value, PatternState state ) : base( state )
            {
                this.value = value;
            }

            #endregion

        }

        public class MultiplierToken : IntToken
        {

            #region Public Properties

            public int count { get; private set; }

            #endregion

            #region Public Constructors

            public MultiplierToken( int value ) : base( value, PatternState.MULTIPLIER )
            {
            }

            #endregion

            #region Public Methods

            public int  IncreaseCount() => ++count;
            public void Reset()         => count = 0;

            #endregion

            #region Private Fields

            #endregion

        }

        #endregion

        #region VALIDATE

        public enum ValidationResult
        {
            VALID,
            EMPTY,
            INDEX_OUT_OF_RANGE,
            MISPLACED_PERIOD,
            MISPLACED_ASTERISK,
            MISPLACED_COMMA,
            UNPAIRED_PARENTHESIS,
            EMPTY_PARENTHESIS,
            INVALID_MULTIPLIER,
            INVALID_CHARACTER,
        }

        public static ValidationResult Validate( string frecuencyPattern, int lastIndex, out Token[] tokens )
        {
            tokens           = null;
            frecuencyPattern = frecuencyPattern.Replace( " ", "" );
            if ( frecuencyPattern == string.Empty )
            {
                return ValidationResult.EMPTY;
            }

            string validCharactersRemoved = Regex.Replace( frecuencyPattern, @"[\d,.*()]", "" );
            if ( validCharactersRemoved != string.Empty )
            {
                return ValidationResult.INVALID_CHARACTER;
            }

            string validBracketsRemoved = Regex.Replace( frecuencyPattern,
                @"\((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!))\)", "" );
            if ( Regex.Match( validBracketsRemoved,
                    @"\(|\)" ).Success )
            {
                return ValidationResult.UNPAIRED_PARENTHESIS;
            }

            if ( Regex.Match( frecuencyPattern,
                    @"\(\)" ).Success )
            {
                return ValidationResult.EMPTY_PARENTHESIS;
            }

            string validMultiplicationsRemoved = Regex.Replace( frecuencyPattern,
                @"(\d+|\))\*\d+", "" );
            if ( Regex.Match( validMultiplicationsRemoved,
                    @"\*" ).Success )
            {
                return ValidationResult.MISPLACED_ASTERISK;
            }

            string validCommasRemoved = Regex.Replace( frecuencyPattern,
                @"(\)|\d+)(,(\(|\d+))+", "" );
            if ( Regex.Match( validCommasRemoved, @"," ).Success )
            {
                return ValidationResult.MISPLACED_COMMA;
            }

            string validDotsRemoved = Regex.Replace( frecuencyPattern, @"(\d|\))\.\.\.(?!.)", "" );
            if ( Regex.Match( validDotsRemoved, @"\." ).Success )
            {
                return ValidationResult.MISPLACED_PERIOD;
            }

            MatchCollection matches   = Regex.Matches( frecuencyPattern, @"\d+|[(),*]|\.\.\." );
            List<Token>     tokenList = new List<Token>();
            tokenList.Add( Token.START );
            foreach ( Match match in matches )
            {
                if ( match.Value == "(" )
                {
                    tokenList.Add( Token.OPENING_PARENTHESIS );
                }
                else if ( match.Value == ")" )
                {
                    tokenList.Add( Token.CLOSING_PARENTHESIS );
                }
                else if ( match.Value == "," )
                {
                    tokenList.Add( Token.COMMA );
                }
                else if ( match.Value == "*" )
                {
                    tokenList.Add( Token.ASTERISK );
                }
                else if ( match.Value == "..." )
                {
                    if ( tokenList.Last() is MultiplierToken )
                    {
                        return ValidationResult.MISPLACED_PERIOD;
                    }

                    tokenList.Add( Token.ELLIPSIS );
                }
                else
                {
                    int value = int.Parse( match.Value );
                    PatternState state = tokenList.Count > 0 && tokenList.Last() == Token.ASTERISK
                        ? PatternState.MULTIPLIER
                        : PatternState.INDEX;
                    if ( state    == PatternState.INDEX
                         && value > lastIndex )
                    {
                        return ValidationResult.INDEX_OUT_OF_RANGE;
                    }

                    if ( state    == PatternState.MULTIPLIER
                         && value < 2 )
                    {
                        return ValidationResult.INVALID_MULTIPLIER;
                    }

                    tokenList.Add( state == PatternState.INDEX ? new IntToken( value, state ) : new MultiplierToken( value ) );
                }
            }

            tokenList.Add( Token.END );
            tokens = tokenList.ToArray();
            return ValidationResult.VALID;
        }

        #endregion

        #region MACHINE

        private Token[]    _tokens;
        private int        _tokenIndex;
        private Stack<int> _parenthesisStack = new Stack<int>();
        private int        _lastParenthesis  = -1;

        public PatternMachine( Token[] tokens )
        {
            _tokens = tokens;
        }

        public void SetTokens( Token[] tokens )
        {
            if ( tokens.SequenceEqual( _tokens ) )
            {
                return;
            }

            _tokens = tokens;
        }

        public void Reset()
        {
            _tokenIndex = 0;
            foreach ( Token token in _tokens )
            {
                if ( token is MultiplierToken )
                {
                    ( token as MultiplierToken ).Reset();
                }
            }
        }

        public int nextIndex
        {
            get
            {
                if ( _tokenIndex == -1 )
                {
                    return -1;
                }

                PatternState currentState = _tokens[ _tokenIndex ].state;
                if ( currentState == PatternState.END )
                {
                    return -1;
                }

                ++_tokenIndex;
                Token nextToken = _tokens[ _tokenIndex ];
                switch ( nextToken.state )
                {
                    case PatternState.INDEX:
                        return ( nextToken as IntToken ).value;
                    case PatternState.OPENING_PARENTHESIS:
                        _parenthesisStack.Push( _tokenIndex );
                        break;
                    case PatternState.CLOSING_PARENTHESIS:
                        _lastParenthesis = _parenthesisStack.Pop();
                        break;
                    case PatternState.MULTIPLIER:
                        MultiplierToken mult = nextToken as MultiplierToken;
                        if ( mult.IncreaseCount() < mult.value )
                        {
                            _tokenIndex = currentState == PatternState.CLOSING_PARENTHESIS
                                ? _lastParenthesis
                                : _tokenIndex - 3;
                        }

                        break;
                    case PatternState.ELLIPSIS:
                        _tokenIndex = currentState == PatternState.CLOSING_PARENTHESIS
                            ? _lastParenthesis - 1
                            : _tokenIndex      - 2;
                        break;
                    case PatternState.END:
                        return -1;
                }

                return nextIndex;
            }
        }

        #endregion

    }
}