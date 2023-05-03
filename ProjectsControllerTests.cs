using IPDImexWebsite.Controllers;
using IPDImexWebsite.Models;
using IPDImexWebsite.Models.Repository;
using IPDImexWebsite.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
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
                    Id = 1,
                    Title= "Test1",
                    Description= "Test1Description",
                },
                new Project
                {
                    Id = 2,
                    Title= "Test2",
                    Description= "Test2Description",
                },
                new Project
                {
                    Id = 3,
                    Title= "Test3",
                    Description= "Test3Description",
                },
                new Project
                {
                    Id = 4,
                    Title= "Test4",
                    Description= "Test4Description",
                }

            };

            foreach(var project in mockData)
            {
                yield return project;
            }
            await Task.CompletedTask;
        }
        public async IAsyncEnumerable<Project> MockGetProjectsEmpty()
        {
            var mockData = new List<Project>
            {
            };

            foreach (var project in mockData)
            {
                yield return project;
            }
            await Task.CompletedTask;
        }
        [Test]
        public async Task Index_ProjectsAreEmpty_RedirectToClientInfo()
        {
            //arrange
            var mockRepository = new Mock<IRepositoryProject>();
            mockRepository.Setup(x => x.GetAllProjects()).Returns(MockGetProjectsEmpty());

            var controller = new ProjectsController(mockRepository.Object);
            controller.PageSize = 2;

            //act
            var result = await controller.Index(1) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/ClientInfo"));
        }

        [Test]
        public async Task Index_CanPaginate_ReturnSpecifiedPage()
        {
            //arrange
            var mockRepository = new Mock<IRepositoryProject>();
            mockRepository.Setup(x => x.GetAllProjects()).Returns(MockGetProjects());

            var controller = new ProjectsController(mockRepository.Object);
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
            mockRepository.Setup(x => x.GetAllProjects()).Returns(MockGetProjects());

            var controller = new ProjectsController(mockRepository.Object);
            controller.PageSize = 2;

            //act
            var result = (await controller.Index(2) as ViewResult)?.ViewData.Model as ProjectsViewModel ?? new();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Pagination.ItemsPerPage, Is.EqualTo(2));
                Assert.That(result.Pagination.CurrentPage, Is.EqualTo(2));
                Assert.That(result.Pagination.TotalItems, Is.EqualTo(4));
                Assert.That(result.Pagination.TotalPages, Is.EqualTo(2));
            });
        }
    }
}
