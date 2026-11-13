using System.Linq;
using Atlas.Common.AzureStorage.Blob;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.Common.Test.AzureStorage.Blob
{
    [TestFixture]
    internal class BatchedResultsFileNamesTests
    {
        private const string Folder = "search-id";

        [TestCase(1, "search-id/000001.json")]
        [TestCase(10, "search-id/000010.json")]
        [TestCase(999999, "search-id/999999.json")]
        [TestCase(1000000, "search-id/1000000.json")]
        public void FileName_PadsFileNumberWithZeros(int fileNumber, string expectedFileName)
        {
            BatchedResultsFileNames.FileName(Folder, fileNumber).Should().Be(expectedFileName);
        }

        [Test]
        public void FileName_WithNestedFolder_KeepsFolder()
        {
            BatchedResultsFileNames.FileName("search-id/repeat-id", 2).Should().Be("search-id/repeat-id/000002.json");
        }

        [Test]
        public void FileName_ForTwelveFiles_NameOrderIsNumberOrder()
        {
            var fileNames = Enumerable.Range(1, 12).Select(n => BatchedResultsFileNames.FileName(Folder, n)).ToList();

            fileNames.Order(System.StringComparer.Ordinal).Should().Equal(fileNames);
        }

        [Test]
        public void OrderByFileNumber_OldNamesWithoutZerosInListingOrder_ReturnsNumberOrder()
        {
            // Blob listing order for 12 files named without zeros.
            var listed = new[] { 1, 10, 11, 12, 2, 3, 4, 5, 6, 7, 8, 9 }.Select(n => $"{Folder}/{n}.json");

            var ordered = BatchedResultsFileNames.OrderByFileNumber(listed);

            ordered.Should().Equal(Enumerable.Range(1, 12).Select(n => $"{Folder}/{n}.json"));
        }

        [Test]
        public void OrderByFileNumber_PaddedNamesInReverseOrder_ReturnsNumberOrder()
        {
            var expected = Enumerable.Range(1, 12).Select(n => BatchedResultsFileNames.FileName(Folder, n)).ToList();

            var ordered = BatchedResultsFileNames.OrderByFileNumber(Enumerable.Reverse(expected));

            ordered.Should().Equal(expected);
        }

        [Test]
        public void OrderByFileNumber_NumberAboveFileNumberWidth_ComesAfterPaddedNames()
        {
            var listed = new[] { $"{Folder}/1000000.json", $"{Folder}/000002.json", $"{Folder}/999999.json" };

            var ordered = BatchedResultsFileNames.OrderByFileNumber(listed);

            ordered.Should().Equal($"{Folder}/000002.json", $"{Folder}/999999.json", $"{Folder}/1000000.json");
        }

        [Test]
        public void OrderByFileNumber_NamesWithoutNumber_ComeLastInNameOrder()
        {
            var listed = new[] { $"{Folder}/b.json", $"{Folder}/10.json", $"{Folder}/a.json", $"{Folder}/2.json" };

            var ordered = BatchedResultsFileNames.OrderByFileNumber(listed);

            ordered.Should().Equal($"{Folder}/2.json", $"{Folder}/10.json", $"{Folder}/a.json", $"{Folder}/b.json");
        }
    }
}
