using Atlas.Client.Models.Search;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Services.DonorUpdates;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.DonorImport.Test.Services
{
    [TestFixture]
    internal class ManuallyPublishDonorUpdatesServiceTest
    {
        private IDonorReadRepository donorReadRepository;
        private IDonorUpdatesSaver donorUpdatesSaver;

        private IManuallyPublishDonorUpdatesService donorUpdatesService;


        [SetUp]
        public void SetUp()
        {
            donorReadRepository = Substitute.For<IDonorReadRepository>();
            donorUpdatesSaver = Substitute.For<IDonorUpdatesSaver>();

            donorUpdatesService = new ManuallyPublishDonorUpdatesService(donorReadRepository, donorUpdatesSaver);

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
        public void PublishDonorUpdates_For_ExistingDonors()
        {
            var input = new[] { 1, 3 };
            IReadOnlyCollection<SearchableDonorUpdate> savedData = null;

            donorUpdatesSaver.WhenForAnyArgs(x => x.Save(default)).Do(x => savedData = x.Arg<IReadOnlyCollection<SearchableDonorUpdate>>());

            donorUpdatesService.PublishDonorUpdates(input);
            donorReadRepository.Received(1).GetDonorsByIds(Arg.Is<ICollection<int>>(value => IsCollectionsEqual(value, input)));

            donorUpdatesSaver.ReceivedWithAnyArgs(1).Save(default);

            var data = savedData.OrderBy(x => x.DonorId).ToArray();
            CheckUpdateMessage(1, data[0], DonorType.Adult);
            CheckUpdateMessage(3, data[1], DonorType.Cord);

        }

        [Test]
        public void PublishDonorUpdates_For_NonExistingDonors()
        {
            var input = new[] { 5, 6 };
            IReadOnlyCollection<SearchableDonorUpdate> savedData = null;

            donorUpdatesSaver.WhenForAnyArgs(x => x.Save(default)).Do(x => savedData = x.Arg<IReadOnlyCollection<SearchableDonorUpdate>>());
            donorUpdatesService.PublishDonorUpdates(input);
            donorReadRepository.Received(1).GetDonorsByIds(Arg.Is<ICollection<int>>(value => IsCollectionsEqual(value, input)));

            donorUpdatesSaver.ReceivedWithAnyArgs(1).Save(default);

            var data = savedData.OrderBy(x => x.DonorId).ToArray();
            CheckDeletionUpdateMessage(5, data[0]);
            CheckDeletionUpdateMessage(6, data[1]);
        }

        [Test]
        public void PublishDonorUpdates_For_Mixed_Existing_And_NonExistingDonors()
        {
            var input = new[] { 5, 6, 1, 2, 3 };
            IReadOnlyCollection<SearchableDonorUpdate> savedData = null;

            donorUpdatesSaver.WhenForAnyArgs(x => x.Save(default)).Do(x => savedData = x.Arg<IReadOnlyCollection<SearchableDonorUpdate>>());
            donorUpdatesService.PublishDonorUpdates(input);
            donorReadRepository.Received(1).GetDonorsByIds(Arg.Is<ICollection<int>>(value => IsCollectionsEqual(value, input)));

            donorUpdatesSaver.ReceivedWithAnyArgs(1).Save(default);

            var data = savedData.OrderBy(x => x.DonorId).ToArray();

            CheckUpdateMessage(1, data[0], DonorType.Adult);
            CheckUpdateMessage(2, data[1], DonorType.Adult);
            CheckUpdateMessage(3, data[2], DonorType.Cord);

            CheckDeletionUpdateMessage(5, data[3]);
            CheckDeletionUpdateMessage(6, data[4]);
        }

        [Test]
        public void PublishDonorUpdates_SplitIntoBatches()
        {
            var input = Enumerable.Range(1, 2000).ToArray();

            List<IReadOnlyCollection<SearchableDonorUpdate>> savedData = new List<IReadOnlyCollection<SearchableDonorUpdate>>();

            donorUpdatesSaver.WhenForAnyArgs(x => x.Save(default)).Do(x => savedData.Add(x.Arg<IReadOnlyCollection<SearchableDonorUpdate>>()));
            donorUpdatesService.PublishDonorUpdates(input);
            donorReadRepository.Received(1).GetDonorsByIds(Arg.Is<ICollection<int>>(value => IsCollectionsEqual(value, input)));

            donorUpdatesSaver.ReceivedWithAnyArgs(2).Save(default);

            savedData.Should().HaveCount(2);
            savedData[0].Should().HaveCount(1500);
            savedData[1].Should().HaveCount(500);
        }


        private void CheckDeletionUpdateMessage(int donorId, SearchableDonorUpdate searchableDonorUpdate)
        {
            searchableDonorUpdate.DonorId.Should().Be(donorId);
            searchableDonorUpdate.IsAvailableForSearch.Should().BeFalse();
            searchableDonorUpdate.SearchableDonorInformation.Should().BeNull();
        }

        private void CheckUpdateMessage(int donorId, SearchableDonorUpdate searchableDonorUpdate, DonorType donorType)
        {
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

        private static bool IsCollectionsEqual(ICollection<int> left, ICollection<int> right)
        {
            if (left.Count != right.Count) 
            {
                return false;
            }

            return left.Zip(right).All(x => x.First == x.Second);
        }

        private readonly Dictionary<int, Data.Models.Donor> donorData = new Dictionary<int, Data.Models.Donor>
        {
            {1, new Data.Models.Donor { AtlasId = 1, DonorType = DatabaseDonorType.Adult, ExternalDonorCode = "#1ExtCode", EthnicityCode = "#1Eth", RegistryCode = "#1RC", A_1 = "#1A_1", A_2 = "#1A_2", B_1 = "#1B_1", B_2 = "#1B_2", C_1 = "#1C_1", C_2 = "#1C_2", DPB1_1 = "#1DPB1_1", DPB1_2 = "#1DPB1_2", DQB1_1 = "#1DQB1_1", DQB1_2 = "#1DQB1_2", DRB1_1 = "#1DRB1_1", DRB1_2 = "#1DRB1_2" }},
            {2, new Data.Models.Donor { AtlasId = 2, DonorType = DatabaseDonorType.Adult, ExternalDonorCode = "#2ExtCode", EthnicityCode = "#2Eth", RegistryCode = "#2RC", A_1 = "#2A_1", A_2 = "#2A_2", B_1 = "#2B_1", B_2 = "#2B_2", C_1 = "#2C_1", C_2 = "#2C_2", DPB1_1 = "#2DPB1_1", DPB1_2 = "#2DPB1_2", DQB1_1 = "#2DQB1_1", DQB1_2 = "#2DQB1_2", DRB1_1 = "#2DRB1_1", DRB1_2 = "#2DRB1_2" }},
            {3, new Data.Models.Donor { AtlasId = 3, DonorType = DatabaseDonorType.Cord, ExternalDonorCode = "#3ExtCode", EthnicityCode = "#3Eth", RegistryCode = "#3RC", A_1 = "#3A_1", A_2 = "#3A_2", B_1 = "#3B_1", B_2 = "#3B_2", C_1 = "#3C_1", C_2 = "#3C_2", DPB1_1 = "#3DPB1_1", DPB1_2 = "#3DPB1_2", DQB1_1 = "#3DQB1_1", DQB1_2 = "#3DQB1_2", DRB1_1 = "#3DRB1_1", DRB1_2 = "#3DRB1_2" }},
        };
    }
}
