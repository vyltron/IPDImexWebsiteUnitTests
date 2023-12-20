using Castle.Core.Logging;
using IPDImexWebsite.Models.Repository;
using IPDImexWebsite.ViewComponents;
using IPDImexWebsite.ViewModels;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPDImexWebsiteUnitTests.ViewComponents
{
    [TestFixture]
    internal class AdminInfoViewComponentTests
    {
        [Test]
        public async Task InvokeAsync_CanCountUnreadAplicationsAndMessages()
        {
            //arrange
            var mockMessages = new Mock<IRepositoryMessage>();
            mockMessages.Setup(x => x.GetUnreadMessagesCount()).Returns(async () => await Task.FromResult(10));
            var mockILogger = new Mock<ILogger<AdminInfoViewComponent>>();

            var mockApplications = new Mock<IRepositoryAplication>();
            mockApplications.Setup(x => x.GetUnreadAplicationsCount()).Returns(async () => await Task.FromResult(12));

            var viewComponent = new AdminInfoViewComponent(mockMessages.Object, mockApplications.Object, mockILogger.Object);

            //act
            var result = (await viewComponent.InvokeAsync() as ViewViewComponentResult)?.ViewData?.Model as AdminInfoViewModel ?? new();

            //assert
            Assert.That(result.MessagesUnreadCount, Is.EqualTo(10));
            Assert.That(result.AplicationsUnreadCount, Is.EqualTo(12));
        }
        [Test]
        public async Task InvokeAsync_ThrowsException_ReturnAdminInfo()
        {
            //arrange
            var mockMessages = new Mock<IRepositoryMessage>();
            mockMessages.Setup(x => x.GetUnreadMessagesCount()).Throws(new Exception("error"));
            var mockILogger = new Mock<ILogger<AdminInfoViewComponent>>();

            var mockApplications = new Mock<IRepositoryAplication>();
            mockApplications.Setup(x => x.GetUnreadAplicationsCount()).Returns(async () => await Task.FromResult(12));

            var viewComponent = new AdminInfoViewComponent(mockMessages.Object, mockApplications.Object, mockILogger.Object);

            //act
            var result = (await viewComponent.InvokeAsync() as ViewViewComponentResult)?.ViewData?.Model as AdminInfoViewModel;

            //assert
            mockMessages.Verify(x => x.GetUnreadMessagesCount(), Times.Once());
            Assert.ThrowsAsync<Exception>(async () => await mockMessages.Object.GetUnreadMessagesCount());
            Assert.That(result!.MessagesUnreadCount, Is.EqualTo(0));
            Assert.That(result.AplicationsUnreadCount, Is.EqualTo(0));
        }
    }
}
