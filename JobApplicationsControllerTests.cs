using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IPDImexWebsite.Controllers;
using IPDImexWebsite.Models;
using IPDImexWebsite.Models.Repository;
using IPDImexWebsite.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IPDImexWebsiteUnitTests
{
    [TestFixture]
    internal class JobApplicationsControllerTests
    {
        public async IAsyncEnumerable<Aplication> MockGetApplications()
        {
            var list = new List<Aplication>
            {
                    new Aplication
                    {
                        Id = 1,
                        LastName = "Messi",
                        PostDateTime = DateTime.Now,
                        ClassificationId = 2,
                        FirstName = "Mercedes",
                        Email = "cipri.dragus@yahoo.com",
                        CoverLetter = "Whats up",
                        Phone = "07474343056",
                        Age = 25,
                    },
                    new Aplication
                    {
                        Id = 2,
                        LastName = "Dragus",
                        PostDateTime = DateTime.Now,
                        ClassificationId = 2,
                        FirstName = "Ciprian",
                        Email = "cipri.dragus@yahoo.com",
                        CoverLetter = "Whats up",
                        Phone = "07474343056",
                        Age = 25,
                    },
                    new Aplication
                    {
                        Id= 3,
                        LastName = "Bade",
                        PostDateTime = DateTime.Now,
                        ClassificationId = 2,
                        FirstName = "Gheorghe",
                        Email = "cipri.dragus@yahoo.com",
                        CoverLetter = "Whats up",
                        Phone = "07474343056",
                        Age = 25,
                    },
                    new Aplication
                    {
                        Id= 4,
                        LastName = "Iulia",
                        PostDateTime = DateTime.Now,
                        ClassificationId = 2,
                        FirstName = "Bunoaca",
                        Email = "cipri.dragus@yahoo.com",
                        CoverLetter = "Whats up",
                        Phone = "07474343056",
                        Age = 25,
                    }
            };

            foreach (var aplication in list)
            {
                yield return aplication;
            }
            await Task.CompletedTask;

        }
        public async Task<Aplication?> MockGetAplicationByIdAsyncClassificationUnread(int applicationId)
        {

            return await Task.FromResult(new Aplication
            {
                Id = applicationId,
                LastName = "Petrica",
                PostDateTime = DateTime.Now,
                Classification = new MessageClassification
                {
                    Id = 2,
                    Name = "Unread"
                },
                ClassificationId = 2,
                FirstName = "Mihoc",
                Email = "cipri.dragus@yahoo.com",
                Phone = "0858322",
                Age = 25,
                CoverLetter = "Whats up"
            });

        }
        public async Task<Aplication?> MockGetAplicationByIdAsyncClassificationRead(int applicationId)
        {

            return await Task.FromResult(new Aplication
            {
                Id = applicationId,
                LastName = "Petrica",
                PostDateTime = DateTime.Now,
                Classification = new MessageClassification
                {
                    Id = 1,
                    Name = "Read"
                },
                ClassificationId = 2,
                FirstName = "Mihoc",
                Email = "cipri.dragus@yahoo.com",
                Phone = "0858322",
                Age = 25,
                CoverLetter = "Whats up"
            });

        }


        #region JobApplicationsPanel
        [Test]
        public async Task JobApplicationPanel_CanCountReadAndUnreadApplications()
        {
            //arrange
            var mockAplications = new Mock<IRepositoryAplication>();
            mockAplications.Setup(x => x.GetAllAplications()).Returns(MockGetApplications());
            mockAplications.Setup(x => x.GetAllAplicationsCount()).Returns(Task.FromResult(25));
            mockAplications.Setup(x => x.GetUnreadAplicationsCount()).Returns(Task.FromResult(12));

            var controller = new JobApplicationsController(mockAplications.Object);
            controller.PageSize = 10;

            //act
            var result = (await controller.JobApplicationsPanel(1) as ViewResult)?.ViewData.Model as JobApplicationsViewModel ?? new();

            //assert
            Assert.That(result.TotalApplications, Is.EqualTo(25));
            Assert.That(result.UnreadApplications, Is.EqualTo(12));
        }
        [Test]
        public async Task JobAplications_CanLoadMessages()
        {
            //arrange
            var mockAplications = new Mock<IRepositoryAplication>();
            mockAplications.Setup(x => x.GetAllAplications()).Returns(MockGetApplications());
            mockAplications.Setup(x => x.GetAllAplicationsCount()).Returns(Task.FromResult(25));
            mockAplications.Setup(x => x.GetUnreadAplicationsCount()).Returns(Task.FromResult(12));

            var controller = new JobApplicationsController(mockAplications.Object);
            controller.PageSize = 10;

            //act
            var result = (await controller.JobApplicationsPanel(1) as ViewResult)?.ViewData.Model as JobApplicationsViewModel ?? new();
            var aplicatonsList = result.Applications.ToList();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(aplicatonsList[0].Id, Is.EqualTo(4));
                Assert.That(aplicatonsList[0].LastName, Is.EqualTo("Iulia"));

                Assert.That(aplicatonsList[3].Id, Is.EqualTo(1));
                Assert.That(aplicatonsList[3].LastName, Is.EqualTo("Messi"));
            });
        }

        [Test]
        public async Task JobAplicationsPanel_CanPaginate()
        {
            //arrange
            var mockAplications = new Mock<IRepositoryAplication>();
            mockAplications.Setup(x => x.GetAllAplications()).Returns(MockGetApplications());
            mockAplications.Setup(x => x.GetAllAplicationsCount()).Returns(Task.FromResult(25));
            mockAplications.Setup(x => x.GetUnreadAplicationsCount()).Returns(Task.FromResult(12));


            var controller = new JobApplicationsController(mockAplications.Object);
            controller.PageSize = 2;

            //act
            var result = (await controller.JobApplicationsPanel(2) as ViewResult)?.ViewData.Model as JobApplicationsViewModel ?? new();
            var messagesList = result.Applications.ToList();
            var pagination = result.Pagination;

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(pagination.CurrentPage, Is.EqualTo(2));
                Assert.That(pagination.ItemsPerPage, Is.EqualTo(2));
                Assert.That(pagination.TotalPages, Is.EqualTo(2));

                Assert.That(messagesList[0].Id, Is.EqualTo(2));
                Assert.That(messagesList[0].FirstName, Is.EqualTo("Ciprian"));
                Assert.That(messagesList[1].Id, Is.EqualTo(1));
                Assert.That(messagesList[1].FirstName, Is.EqualTo("Mercedes"));
            });
        }
        #endregion

        #region Applications
        [Test]
        public async Task Applcations_LoadapplicationButIsNull_ReturnAdminInfoPage()
        {
            //arrange
            var mockAplications = new Mock<IRepositoryAplication>();
            mockAplications.Setup(c => c.GetAplicationByIdAsync(It.IsAny<int>())).Returns(async (int applicationId) => await Task.FromResult(default(Aplication)));

            var controller = new JobApplicationsController(mockAplications.Object);

            //act
            var result = await controller.Application(2) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/AdminInfo"));
        }
        [Test]
        public async Task Aplication_CanLoadTheAplications_ReturnAplicationView()
        {
            //arrange
            var mockAplication = new Mock<IRepositoryAplication>();
            mockAplication.Setup(c => c.GetAplicationByIdAsync(It.IsAny<int>())).Returns(MockGetAplicationByIdAsyncClassificationUnread(2));

            var controller = new JobApplicationsController(mockAplication.Object);

            //act
            var result = (await controller.Application(2) as ViewResult)?.ViewData.Model as Aplication ?? new();

            //assert
            Assert.That(result?.Id, Is.EqualTo(2));
            Assert.That(result?.LastName, Is.EqualTo("Petrica"));
            Assert.That(result?.FirstName, Is.EqualTo("Mihoc"));
        }
        [Test]
        public async Task Aplication_CanLoadTheAplicationAndChangeClassificationToRead_ReturnAplicationView()
        {
            //arrange
            var mockAplication = new Mock<IRepositoryAplication>();
            mockAplication.Setup(c => c.GetAplicationByIdAsync(It.IsAny<int>())).Returns(MockGetAplicationByIdAsyncClassificationUnread(2));
            mockAplication.Setup(c => c.MarksAsReadAsync(It.IsAny<Aplication>())).Returns(async () => await Task.FromResult(true));

            var controller = new JobApplicationsController(mockAplication.Object);

            //act
            var result = (await controller.Application(2) as ViewResult)?.ViewData.Model as Aplication ?? new();

            //assert
            mockAplication.Verify(x => x.MarksAsReadAsync(It.IsAny<Aplication>()), Times.Once);
        }
        [Test]
        public async Task Aplication_CanLoadTheAplicationButClassificationIsAlreadyReadSoItWontCallTheMarkAsReadMethod_ReturnAplicationView()
        {
            //arrange
            var mockAplication = new Mock<IRepositoryAplication>();
            mockAplication.Setup(c => c.GetAplicationByIdAsync(It.IsAny<int>())).Returns(MockGetAplicationByIdAsyncClassificationRead(2));

            var controller = new JobApplicationsController(mockAplication.Object);

            //act
            var result = (await controller.Application(2) as ViewResult)?.ViewData.Model as Aplication ?? new();

            //assert
            mockAplication.Verify(x => x.MarksAsReadAsync(It.IsAny<Aplication>()), Times.Never);
        }
        #endregion

        #region DeleteApplication
        [Test]
        public async Task DeleteAplication_AplicationWasDeleted_RedirectToJobAplicationPanel()
        {
            //arrange
            var mock = new Mock<IRepositoryAplication>();
            mock.Setup(x => x.DeleteAplicationAsync(It.IsAny<int>())).Returns(async () => await Task.FromResult(true));

            //act
            var controller = new JobApplicationsController(mock.Object);
            var result = await controller.DeleteApplication(1) as RedirectToActionResult;

            //assert
            Assert.That(result!.ActionName, Is.EqualTo("JobApplicationsPanel"));
        }
        [Test]
        public async Task DeleteAplication_AplicationWasNotDeleted_RedirectToAdminInfo()
        {
            //arrange
            var mock = new Mock<IRepositoryAplication>();
            mock.Setup(x => x.DeleteAplicationAsync(It.IsAny<int>())).Returns(async () => await Task.FromResult(false));

            //act
            var controller = new JobApplicationsController(mock.Object);
            var result = await controller.DeleteApplication(1) as RedirectToPageResult;

            //assert
            Assert.That(result!.PageName, Is.EqualTo("/AdminInfo"));
        }
        #endregion

        #region SearchApplications
        [Test]
        [TestCase(null)]
        [TestCase("")]
        public async Task SearchAplications_SearchCriteriaIsNullOrEmptytReturnJobAplicationsPanel(string searchCriteria)
        {
            //assert
            var mock = new Mock<IRepositoryAplication>();

            var controller = new JobApplicationsController(mock.Object);

            //act
            var result = await controller.SearchApplications(searchCriteria, 1) as RedirectToActionResult;

            //assert
            Assert.That(result!.ActionName, Is.EqualTo("JobApplicationsPanel"));
        }
        [Test]
        public async Task SearchApplications_CanCountReadAndUnreadMessages()
        {
            //arrange
            var mockAplications = new Mock<IRepositoryAplication>();
            mockAplications.Setup(x => x.SearchAsync(It.IsAny<string>())).Returns(MockGetApplications());
            mockAplications.Setup(x => x.GetAllAplicationsCount()).Returns(Task.FromResult(25));
            mockAplications.Setup(x => x.GetUnreadAplicationsCount()).Returns(Task.FromResult(12));


            var controller = new JobApplicationsController(mockAplications.Object);
            controller.PageSize = 10;

            //act
            var result = (await controller.SearchApplications("Test", 1) as ViewResult)?.ViewData.Model as JobApplicationsViewModel ?? new();

            //assert
            Assert.That(result.TotalApplications, Is.EqualTo(25));
            Assert.That(result.UnreadApplications, Is.EqualTo(12));
        }
        [Test]
        public async Task SearchApplications_CanSearchThroughApplications()
        {
            //arrange
            var mockAplications = new Mock<IRepositoryAplication>();
            mockAplications.Setup(x => x.SearchAsync(It.IsAny<string>())).Returns(MockGetApplications());
            mockAplications.Setup(x => x.GetAllAplicationsCount()).Returns(Task.FromResult(25));
            mockAplications.Setup(x => x.GetUnreadAplicationsCount()).Returns(Task.FromResult(12));

            var controller = new JobApplicationsController(mockAplications.Object);
            controller.PageSize = 10;

            //act
            var result = (await controller.SearchApplications("Test", 1) as ViewResult)?.ViewData.Model as JobApplicationsViewModel ?? new();
            var messagesList = result.Applications.ToList();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(messagesList[0].Id, Is.EqualTo(4));
                Assert.That(messagesList[0].LastName, Is.EqualTo("Iulia"));

                Assert.That(messagesList[3].Id, Is.EqualTo(1));
                Assert.That(messagesList[3].LastName, Is.EqualTo("Messi"));
            });
        }
        [Test]
        public async Task SearchApplications_CanPaginate()
        {
            //arrange
            var mockAplications = new Mock<IRepositoryAplication>();
            mockAplications.Setup(x => x.SearchAsync(It.IsAny<string>())).Returns(MockGetApplications());
            mockAplications.Setup(x => x.GetAllAplicationsCount()).Returns(Task.FromResult(25));
            mockAplications.Setup(x => x.GetUnreadAplicationsCount()).Returns(Task.FromResult(12));

            var controller = new JobApplicationsController(mockAplications.Object);
            controller.PageSize = 2;

            
            //act
            var result = (await controller.SearchApplications("Test", 2) as ViewResult)?.ViewData.Model as JobApplicationsViewModel ?? new();
            var messagesList = result.Applications.ToList();
            var pagination = result.Pagination;

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(pagination.CurrentPage, Is.EqualTo(2));
                Assert.That(pagination.ItemsPerPage, Is.EqualTo(2));
                Assert.That(pagination.TotalPages, Is.EqualTo(2));

                Assert.That(messagesList[0].Id, Is.EqualTo(2));
                Assert.That(messagesList[0].FirstName, Is.EqualTo("Ciprian"));
                Assert.That(messagesList[1].Id, Is.EqualTo(1));
                Assert.That(messagesList[1].FirstName, Is.EqualTo("Mercedes"));
            });
        }
        [Test]
        public async Task SearchApplications_CanSendSearchRequest()
        {
            //arrange
            var mockAplications = new Mock<IRepositoryAplication>();
            mockAplications.Setup(x => x.SearchAsync(It.IsAny<string>())).Returns(MockGetApplications());
            mockAplications.Setup(x => x.GetAllAplicationsCount()).Returns(Task.FromResult(25));
            mockAplications.Setup(x => x.GetUnreadAplicationsCount()).Returns(Task.FromResult(12));

            var controller = new JobApplicationsController(mockAplications.Object);
            controller.PageSize = 2;

            //act
            var result = (await controller.SearchApplications("Test", 2) as ViewResult)?.ViewData.Model as JobApplicationsViewModel ?? new();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result.SearchRequest, Is.EqualTo(true));
                Assert.That(result.SearchCriteria, Is.EqualTo("Test"));
            });
        }
        #endregion
    }
}
