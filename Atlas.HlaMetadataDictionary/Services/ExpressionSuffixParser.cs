namespace Atlas.HlaMetadataDictionary.Services
{
    internal static class ExpressionSuffixParser
    {
        public static string GetExpressionSuffix(string name)
        {
            var finalCharacter = name[name.Length - 1];
            return char.IsUpper(finalCharacter) ? finalCharacter.ToString() : "";
        }
        
        public static bool IsAlleleNull(string name)
        {
            var finalCharacter = name[name.Length - 1];
            return NullExpressionSuffix == finalCharacter;
        }

        private const char NullExpressionSuffix = 'N';
    }
}