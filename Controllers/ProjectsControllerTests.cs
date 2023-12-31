using Castle.Core.Logging;
using IPDImexWebsite.Controllers;
using IPDImexWebsite.Models;
using IPDImexWebsite.Models.Repository;
using IPDImexWebsite.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NuGet.Frameworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace IPDImexWebsiteUnitTests
{
    [TestFixture]
    internal class ProjectsControllerTests
    {
        public async IAsyncEnumerable<Project> MockGetProjects()
        {
            var mockData = new List<Project> 
            {
                new Project
                {
                    Id = 2,
                    Title= "Test2",
                    Description= "Test2Description",
                },
                new Project
                {
                    Id = 1,
                    Title= "Test1",
                    Description= "Test1Description",
                },
            };

            foreach(var project in mockData)
            {
                yield return project;
            }
            await Task.CompletedTask;
        }

        [Test]
        public async Task Index_CanPaginate_ReturnSpecifiedPage()
        {
            //arrange
            var mockRepository = new Mock<IRepositoryProject>();
            mockRepository.Setup(x => x.GetProjectsWithPaginationInDescendingOrder(It.IsAny<int>(), It.IsAny<int>())).Returns(MockGetProjects());
            var mockILogger = new Mock<ILogger<ProjectsController>>();
            var mockHttp = new Mock<IHttpContextAccessor>();

            var controller = new ProjectsController(mockRepository.Object, mockILogger.Object, mockHttp.Object);
            controller.PageSize = 2;

            //act
            var result = (await controller.Index(2) as ViewResult)?.ViewData.Model as ProjectsViewModel ?? new();
            var projects = result.Projects!.ToList();    

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(projects, Has.Count.EqualTo(2));
                Assert.That(projects[0].Id, Is.EqualTo(2));
                Assert.That(projects[0].Title, Is.EqualTo("Test2"));
                Assert.That(projects[0].Description, Is.EqualTo("Test2Description"));
                Assert.That(projects[1].Id, Is.EqualTo(1));
                Assert.That(projects[1].Title, Is.EqualTo("Test1"));
                Assert.That(projects[1].Description, Is.EqualTo("Test1Description"));
            });
        }
        [Test]
        public async Task Index_PaginationHasTheRightProperties_PaginationObjectOK()
        {
            //arrange
            var mockRepository = new Mock<IRepositoryProject>();
            mockRepository.Setup(x => x.GetProjectsWithPaginationInDescendingOrder(It.IsAny<int>(), It.IsAny<int>())).Returns(MockGetProjects());
            mockRepository.Setup(x => x.GetProjectsCount()).Returns(async () => await Task.FromResult(2));
            var mockILogger = new Mock<ILogger<ProjectsController>>();
            var mockHttp = new Mock<IHttpContextAccessor>();
            var controller = new ProjectsController(mockRepository.Object, mockILogger.Object, mockHttp.Object);
            controller.PageSize = 2;

            //act
            var result = await controller.Index(2) as ViewResult;
            var model = result?.ViewData?.Model as ProjectsViewModel ?? new();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(model.Pagination.ItemsPerPage, Is.EqualTo(2));
                Assert.That(model.Pagination.CurrentPage, Is.EqualTo(2));
                Assert.That(model.Pagination.TotalItems, Is.EqualTo(2));
                Assert.That(model.Pagination.TotalPages, Is.EqualTo(1));
                Assert.That(result?.ViewData.ContainsKey("CurrentUrl"), Is.True);

            });
        }

        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        public async Task ReadProject_IdIsLessThanZeor_ReturnIndexAction(int projectId)
        {
            //arraange
            var mockRepository = new Mock<IRepositoryProject>();
            var mockILogger = new Mock<ILogger<ProjectsController>>();
            var mockHttp = new Mock<IHttpContextAccessor>();
            var controller = new ProjectsController(mockRepository.Object, mockILogger.Object, mockHttp.Object);

            //act
            var result = await controller.ReadProject(projectId) as RedirectToActionResult;

            //assert
            Assert.That(result?.ActionName, Is.EqualTo("Index"));
            mockRepository.Verify(x => x.GetProjectIncludePictures(It.IsAny<int>()),Times.Never());
        }

        [Test]
     
        public async Task ReadProject_ProjectIsNotFound_ReturnClientInfo()
        {
            //arraange
            var mockRepository = new Mock<IRepositoryProject>();
            Project project = default!;
            mockRepository.Setup(x => x.GetProjectIncludePictures(It.IsAny<int>())).ReturnsAsync(project);
            var mockILogger = new Mock<ILogger<ProjectsController>>();
            var mockHttp = new Mock<IHttpContextAccessor>();
            var controller = new ProjectsController(mockRepository.Object, mockILogger.Object, mockHttp.Object);

            //act
            var result = await controller.ReadProject(2) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/ClientInfo"));
            mockRepository.Verify(x => x.GetProjectIncludePictures(It.IsAny<int>()), Times.Once());
        }
        [Test]

        public async Task ReadProject_ProjectIsFound_ReturnView()
        {
            //arraange
            var mockRepository = new Mock<IRepositoryProject>();
            Project project = new Project { Id = 2, Title="TestTitle" };
            mockRepository.Setup(x => x.GetProjectIncludePictures(It.IsAny<int>())).ReturnsAsync(project);
            var mockILogger = new Mock<ILogger<ProjectsController>>();
            var mockHttp = new Mock<IHttpContextAccessor>();
            var controller = new ProjectsController(mockRepository.Object, mockILogger.Object, mockHttp.Object);

            //act
            var result = await controller.ReadProject(2) as ViewResult;
            var model = result?.ViewData.Model as Project ?? new();

            //assert
            Assert.That(model.Id, Is.EqualTo(2));
            Assert.That(model.Title, Is.EqualTo("TestTitle"));
            mockRepository.Verify(x => x.GetProjectIncludePictures(It.IsAny<int>()), Times.Once());
        }
    }
}
