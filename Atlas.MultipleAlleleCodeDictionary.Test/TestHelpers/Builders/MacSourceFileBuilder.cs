using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;

namespace Atlas.MultipleAlleleCodeDictionary.Test.TestHelpers.Builders
{
    internal static class MacSourceFileBuilder
    {
        public static Stream BuildMacFile(params Mac[] macEntities)
        {
            return BuildMacFile(macEntities.ToList());
        }
        
        public static Stream BuildMacFile(IEnumerable<Mac> macEntities)
        {
            var file = BuildFileAsString(macEntities);
            return file.ToStream();
        }

        private static string BuildFileAsString(IEnumerable<Mac> macEntities)
        {
            var fileBuilder = new StringBuilder();
            fileBuilder.AppendLine("LAST UPDATED: 10/03/19");
            fileBuilder.AppendLine("*    CODE    SUBTYPE");
            fileBuilder.Append(Environment.NewLine);
            foreach (var macEntity in macEntities)
            {
                if (!macEntity.IsGeneric)
                {
                    fileBuilder.Append("*");
                }

                fileBuilder.Append("\t");
                fileBuilder.Append(macEntity.Code);
                fileBuilder.Append("\t");
                fileBuilder.Append(macEntity.Hla);
                fileBuilder.Append(Environment.NewLine);
            }
            return fileBuilder.ToString();
        }
    }
}