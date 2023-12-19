using Castle.Core.Logging;
using IPDImexWebsite.Controllers;
using IPDImexWebsite.Models;
using IPDImexWebsite.Models.Repository;
using IPDImexWebsite.ViewModels;
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
            mockRepository.Setup(x => x.GetProjectsIncludePicturesWithPaginationInDescendingOrder(It.IsAny<int>(), It.IsAny<int>())).Returns(MockGetProjects());
            var mockILogger = new Mock<ILogger<ProjectsController>>();

            var controller = new ProjectsController(mockRepository.Object, mockILogger.Object);
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
            mockRepository.Setup(x => x.GetProjectsIncludePicturesWithPaginationInDescendingOrder(It.IsAny<int>(), It.IsAny<int>())).Returns(MockGetProjects());
            mockRepository.Setup(x => x.GetProjectsCount()).Returns(async () => await Task.FromResult(2));
            var mockILogger = new Mock<ILogger<ProjectsController>>();

            var controller = new ProjectsController(mockRepository.Object, mockILogger.Object);
            controller.PageSize = 2;

            //act
            var result = (await controller.Index(2) as ViewResult)?.ViewData.Model as ProjectsViewModel ?? new();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Pagination.ItemsPerPage, Is.EqualTo(2));
                Assert.That(result.Pagination.CurrentPage, Is.EqualTo(2));
                Assert.That(result.Pagination.TotalItems, Is.EqualTo(2));
                Assert.That(result.Pagination.TotalPages, Is.EqualTo(1));
            });
        }
    }
}
