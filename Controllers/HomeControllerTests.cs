using IPDImexWebsite.Controllers;
using IPDImexWebsite.Models.Repository;
using Moq;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

namespace IPDImexWebsiteUnitTests.Controllers
{
    [TestFixture]
    internal class HomeControllerTests
    {
        [Test]
        public async Task Index_CanGetLastProject_ReturnsView()
        {
            //arrange
            var mockProject = new Mock<IRepositoryProject>();
            mockProject.Setup(x => x.GetLastProjectPosted()).Returns(async () => await Task.FromResult(new Project { Id = 1, Title = "TestT", Description = "TestD" }));

            var mockLog = new Mock<ILogger<HomeController>>();
            var controller = new HomeController(mockProject.Object, mockLog.Object);

            //act
            var result = await controller.Index() as ViewResult ?? new();
            var project = result!.ViewData.Model as Project ?? new();

            //assert
            Assert.That(project.Id, Is.EqualTo(1));
            Assert.That(project.Title, Is.EqualTo("TestT"));
        }

        [Test]
        public async Task Index_LastProjectIsNull_ReturnsView()
        {
            //arrange
            var mockProject = new Mock<IRepositoryProject>();
            mockProject.Setup(x => x.GetLastProjectPosted()).Returns(async () => await Task.FromResult(default(Project)));

            var mockLog = new Mock<ILogger<HomeController>>();
            var controller = new HomeController(mockProject.Object, mockLog.Object);

            //act
            var result = await controller.Index() as ViewResult ?? new();
            Project? project = result!.ViewData.Model as Project;

            //assert
            Assert.That(project, Is.Null);
        }
    }
}
