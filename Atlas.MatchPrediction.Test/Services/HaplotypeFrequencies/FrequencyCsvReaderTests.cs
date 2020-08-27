using System;
using System.IO;
using System.Text;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.HaplotypeFrequencies
{
    public class FrequencyCsvReaderTests
    {
        private const string CsvHeader = "a;b;c;drb1;dqb1;population_id;freq;nomenclature_version;population_id;don_pool;ethn";
        private const string CsvFileBodySingleFrequency = "A-HLA;B-HLA;C-HLA;DRB1-HLA;DQBQ-HLA;1;0.00001;3330;1;Reg;Eth";

        private IFrequencyJsonReader reader;

        [SetUp]
        public void Setup()
        {
            reader = new FrequencyJsonReader();
        }

        [Test]
        public void GetFrequencies_StreamIsNull_ThrowsException()
        {
            reader.Invoking(service => service.GetFrequencies(null)).Should().Throw<Exception>();
        }

        [Test]
        public void GetFrequencies_FileDoesNotFollowExpectedSchema_ThrowsException()
        {
            reader.Invoking(service =>
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("invalid-file")))
                {
                    return service.GetFrequencies(stream);
                }
            }).Should().Throw<HaplotypeFormatException>();
        }

        [Test]
        public void GetFrequencies_ReadsFrequencies()
        {
            const int count = 5;
            var csvFile = CsvFileBuilder(count);

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvFile)))
            {
                var frequencies = reader.GetFrequencies(stream);
                frequencies.Should().Be(count);
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