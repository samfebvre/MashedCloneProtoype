using System;
using System.IO;

namespace Editor
{
    public static class StringExtensions
    {

        #region Public Methods

        public static int GetLineNumber( this string text, string lineToFind, StringComparison comparison = StringComparison.CurrentCulture )
        {
            int lineNumber = 0;

            using ( StringReader reader = new StringReader( text ) )
            {
                string line;

                while ( ( line = reader.ReadLine() ) != null )
                {
                    lineNumber++;

                    if ( line.Equals( lineToFind, comparison ) )
                    {
                        return lineNumber;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        ///     Returns how many words there are in the string
        /// </summary>
        public static int GetWordCount( this string text )
        {
            int numberOfWords = 0;

            for ( int i = 0; i < text.Length; i++ )
            {
                if ( text[ i ]    == ' '
                     || text[ i ] == '\n'
                     || text[ i ] == '\t' )
                {
                    numberOfWords++;
                }
            }

            return numberOfWords;
        }

        /// <summary>
        ///     Flipped version of <see cref="string.IsNullOrEmpty" /> for convenience.<br />
        ///     Will NOT throw a NullReferenceException on NULL strings
        /// </summary>
        /// <returns>True if the string is neither null nor empty</returns>
        public static bool IsNotNullOrEmpty( this string value ) => string.IsNullOrEmpty( value ) == false;

        #endregion

    }
}