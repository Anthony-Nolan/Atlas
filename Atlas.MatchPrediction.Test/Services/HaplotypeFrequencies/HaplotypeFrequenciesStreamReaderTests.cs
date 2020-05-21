using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Atlas.MatchPrediction.Test.Services.HaplotypeFrequencies
{
    public class HaplotypeFrequenciesStreamReaderTests
    {
        private const string CsvHeader = "a;b;c;drb1;dqb1;population_id;freq";
        private const string CsvFileBodySingleFrequency = "A-HLA;B-HLA;C-HLA;DRB1-HLA;DQBQ-HLA;1;0.00001";

        private IHaplotypeFrequenciesStreamReader reader;

        [SetUp]
        public void Setup()
        {
            reader = new HaplotypeFrequencyCsvFileReader();
        }

        [Test]
        public void GetFrequencies_StreamIsNull_ThrowsException()
        {
            reader.Invoking(service => service.GetFrequencies(null, 1, 0))
                .Should().Throw<Exception>();
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void GetFrequencies_BatchSizeIsInvalid_ThrowsException(int batchSize)
        {
            reader.Invoking(service => service.GetFrequencies(Stream.Null, batchSize, 0))
                .Should().Throw<Exception>();
        }

        [Test]
        public void GetFrequencies_FileDoesNotFollowExpectedSchema_ReturnsEmptyCollection()
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("invalid-file")))
            {
                var frequencies = reader.GetFrequencies(stream, 100, 0);
                frequencies.Should().BeEmpty();
            }
        }

        [Test]
        public void GetFrequencies_ReadsFrequencies()
        {
            const int count = 5;
            var csvFile = CsvFileBuilder(count);

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvFile)))
            {
                var frequencies = reader.GetFrequencies(stream, 100, 0);
                frequencies.Count().Should().Be(count);
            }
        }

        [Test]
        public void GetFrequencies_ReadsBatchOfExpectedSize()
        {
            var csvFile = CsvFileBuilder(5);

            const int batchSize = 2;

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvFile)))
            {
                var frequencies = reader.GetFrequencies(stream, batchSize, 0);
                frequencies.Count().Should().Be(batchSize);
            }
        }

        [Test]
        public void GetFrequencies_SkipsToStartingPoint()
        {
            const int startingPoint = 1;
            const int count = 5;
            var csvFile = CsvFileBuilder(count);

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvFile)))
            {
                var frequencies = reader.GetFrequencies(stream, 100, startingPoint);
                frequencies.Count().Should().Be(count - startingPoint);
            }
        }

        [Test]
        public void GetFrequencies_StartingPointPastEndOfFile_ReturnsEmptyCollection()
        {
            const int startingPoint = 5;
            const int count = 5;
            var csvFile = CsvFileBuilder(count);

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvFile)))
            {
                var frequencies = reader.GetFrequencies(stream, 100, startingPoint);
                frequencies.Should().BeEmpty();
            }
        }

        [Test]
        public void GetFrequencies_BatchSizeGreaterThanTotalCount_ReadsAllFrequencies()
        {
            const int count = 5;
            var csvFile = CsvFileBuilder(count);

            const int batchSize = count + 1;

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvFile)))
            {
                var frequencies = reader.GetFrequencies(stream, batchSize, 0);
                frequencies.Count().Should().Be(count);
            }
        }

        [Test]
        public void GetFrequencies_BatchSizeGreaterThanRemainingCount_ReadsRemainingFrequencies()
        {
            const int count = 5;
            var csvFile = CsvFileBuilder(count);

            const int startFrom = 2;
            const int batchSize = 100;

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvFile)))
            {
                var frequencies = reader.GetFrequencies(stream, batchSize, startFrom);
                frequencies.Count().Should().Be(count - startFrom);
            }
        }

        private static string CsvFileBuilder(int frequencyCount)
        {
            var file = new StringBuilder(CsvHeader + Environment.NewLine);

            for (var i = 0; i < frequencyCount; i++)
            {
                file.AppendLine(CsvFileBodySingleFrequency);
            }

            return file.ToString();
        }
    }
}