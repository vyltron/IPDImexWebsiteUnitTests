using IPDImexWebsite.Models.Repository;
using IPDImexWebsite.ViewComponents;
using IPDImexWebsite.ViewModels;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPDImexWebsiteUnitTests
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

            var mockApplications = new Mock<IRepositoryAplication>();
            mockApplications.Setup(x => x.GetUnreadAplicationsCount()).Returns(async () => await Task.FromResult(12));

            var viewComponent = new AdminInfoViewComponent(mockMessages.Object, mockApplications.Object);

            //act
            var result = (await viewComponent.InvokeAsync() as ViewViewComponentResult)?.ViewData?.Model as AdminInfoViewModel ?? new();

            //assert
            Assert.That(result.MessagesUnreadCount, Is.EqualTo(10));
            Assert.That(result.AplicationsUnreadCount, Is.EqualTo(12));
        }
    }
}
