using Atlas.Client.Models.Search;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Services.DonorUpdates;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using FluentAssertions;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.DonorImport.Test.Services.DonorUpdates
{
    [TestFixture]
    internal class DonorUpdatesSaveTests
    {
        private IDonorReadRepository donorReadRepository;
        private IPublishableDonorUpdatesRepository publishableDonorUpdatesRepository;

        private DonorUpdatesSaver donorUpdatesSaver;


        [SetUp]
        public void SetUp()
        {
            donorReadRepository = Substitute.For<IDonorReadRepository>();
            publishableDonorUpdatesRepository = Substitute.For<IPublishableDonorUpdatesRepository>();


            donorUpdatesSaver = new DonorUpdatesSaver(publishableDonorUpdatesRepository, donorReadRepository);

            donorReadRepository.GetDonorsByIds(Arg.Any<ICollection<int>>()).Returns(call =>
            {
                var input = call.Arg<ICollection<int>>();
                return input
                    .Where(x => donorData.ContainsKey(x))
                    .Select(x => donorData[x])
                    .ToDictionary(x => x.AtlasId, x => x);
            });
        }


        [Test]
        public async Task GenerateAndSave_ForExistingDonors_SavesUpdatesMessages()
        {
            var input = new[] { 1, 3 };
            IReadOnlyCollection<PublishableDonorUpdate> savedData = null;
            publishableDonorUpdatesRepository.WhenForAnyArgs(x => x.BulkInsert(default)).Do(x => savedData = x.Arg<IReadOnlyCollection<PublishableDonorUpdate>>());

            await donorUpdatesSaver.GenerateAndSave(input);

            await donorReadRepository.Received(1).GetDonorsByIds(Arg.Is<ICollection<int>>(value => value.SequenceEqual(input)));
            await publishableDonorUpdatesRepository.ReceivedWithAnyArgs(1).BulkInsert(default);
            var data = savedData.OrderBy(x => x.DonorId).ToArray();
            CheckUpdateMessage(1, data[0], DonorType.Adult);
            CheckUpdateMessage(3, data[1], DonorType.Cord);
        }

        [Test]
        public async Task GenerateAndSave_ForNonExistingDonors_SavesUpdatesMessages()
        {
            var input = new[] { 5, 6 };
            IReadOnlyCollection<PublishableDonorUpdate> savedData = null;
            publishableDonorUpdatesRepository.WhenForAnyArgs(x => x.BulkInsert(default)).Do(x => savedData = x.Arg<IReadOnlyCollection<PublishableDonorUpdate>>());

            await donorUpdatesSaver.GenerateAndSave(input);

            await donorReadRepository.Received(1).GetDonorsByIds(Arg.Is<ICollection<int>>(value => value.SequenceEqual(input)));
            await publishableDonorUpdatesRepository.ReceivedWithAnyArgs(1).BulkInsert(default);
            var data = savedData.OrderBy(x => x.DonorId).ToArray();
            CheckDeletionUpdateMessage(5, data[0]);
            CheckDeletionUpdateMessage(6, data[1]);
        }

        [Test]
        public async Task GenerateAndSave_ForMixedExistingAndNonExistingDonors_SavesBothUpdatesAndDeletionMessages()
        {
            var input = new[] { 5, 6, 1, 2, 3 };
            IReadOnlyCollection<PublishableDonorUpdate> savedData = null;
            publishableDonorUpdatesRepository.WhenForAnyArgs(x => x.BulkInsert(default)).Do(x => savedData = x.Arg<IReadOnlyCollection<PublishableDonorUpdate>>());

            await donorUpdatesSaver.GenerateAndSave(input);

            await donorReadRepository.Received(1).GetDonorsByIds(Arg.Is<ICollection<int>>(value => value.SequenceEqual(input)));
            await publishableDonorUpdatesRepository.ReceivedWithAnyArgs(1).BulkInsert(default);

            var data = savedData.OrderBy(x => x.DonorId).ToArray();
            CheckUpdateMessage(1, data[0], DonorType.Adult);
            CheckUpdateMessage(2, data[1], DonorType.Adult);
            CheckUpdateMessage(3, data[2], DonorType.Cord);
            CheckDeletionUpdateMessage(5, data[3]);
            CheckDeletionUpdateMessage(6, data[4]);
        }


        private void CheckDeletionUpdateMessage(int donorId, PublishableDonorUpdate publishableDonorUpdate)
        {
            publishableDonorUpdate.DonorId.Should().Be(donorId);

            var searchableDonorUpdate = JsonConvert.DeserializeObject<SearchableDonorUpdate>(publishableDonorUpdate.SearchableDonorUpdate);

            searchableDonorUpdate.DonorId.Should().Be(donorId);
            searchableDonorUpdate.IsAvailableForSearch.Should().BeFalse();
            searchableDonorUpdate.SearchableDonorInformation.Should().BeNull();
        }

        private void CheckUpdateMessage(int donorId, PublishableDonorUpdate publishableDonorUpdate, DonorType donorType)
        {
            publishableDonorUpdate.DonorId.Should().Be(donorId);

            var searchableDonorUpdate = JsonConvert.DeserializeObject<SearchableDonorUpdate>(publishableDonorUpdate.SearchableDonorUpdate);

            searchableDonorUpdate.DonorId.Should().Be(donorId);
            searchableDonorUpdate.IsAvailableForSearch.Should().BeTrue();
            searchableDonorUpdate.SearchableDonorInformation.Should().NotBeNull();

            var info = searchableDonorUpdate.SearchableDonorInformation;
            info.DonorId.Should().Be(donorId);
            info.DonorType.Should().Be(donorType);
            info.ExternalDonorCode.Should().Be($"#{donorId}ExtCode");
            info.EthnicityCode.Should().Be($"#{donorId}Eth");
            info.RegistryCode.Should().Be($"#{donorId}RC");
            info.A_1.Should().Be($"#{donorId}A_1");
            info.A_2.Should().Be($"#{donorId}A_2");
            info.B_1.Should().Be($"#{donorId}B_1");
            info.B_2.Should().Be($"#{donorId}B_2");
            info.C_1.Should().Be($"#{donorId}C_1");
            info.C_2.Should().Be($"#{donorId}C_2");
            info.DPB1_1.Should().Be($"#{donorId}DPB1_1");
            info.DPB1_2.Should().Be($"#{donorId}DPB1_2");
            info.DQB1_1.Should().Be($"#{donorId}DQB1_1");
            info.DQB1_2.Should().Be($"#{donorId}DQB1_2");
            info.DRB1_1.Should().Be($"#{donorId}DRB1_1");
            info.DRB1_2.Should().Be($"#{donorId}DRB1_2");
        }

        private static readonly Dictionary<int, Data.Models.Donor> donorData = new Dictionary<int, Data.Models.Donor>
        {
            {1, DatabaseDonorBuilder.WithPropValuesBasedOnId(1, DatabaseDonorType.Adult)},
            {2, DatabaseDonorBuilder.WithPropValuesBasedOnId(2, DatabaseDonorType.Adult)},
            {3, DatabaseDonorBuilder.WithPropValuesBasedOnId(3, DatabaseDonorType.Cord)},
        };
    }
}
