﻿using IPDImexWebsite.Models.Repository;
using IPDImexWebsite.ViewComponents;
using IPDImexWebsite.ViewModels;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPDImexWebsiteUnitTests
{
    [TestFixture]
    internal class ControlPanelViewComponentTests
    {
        [Test]
        public async Task InvokeAsync_CanCounAllAplicationsAndMessages()
        {
            //arrange
            var mockMessages = new Mock<IRepositoryMessage>();
            mockMessages.Setup(x => x.GetAllMessagesCount()).Returns(async () => await Task.FromResult(10));

            var mockApplications = new Mock<IRepositoryAplication>();
            mockApplications.Setup(x => x.GetAllAplicationsCount()).Returns(async () => await Task.FromResult(12));

            var mockJobs = new Mock<IRepositoryJob>();
            mockJobs.Setup(x => x.GetJobsCountAsync()).Returns(async () => await Task.FromResult(15));

            var viewComponent = new ControlPanelViewComponent(mockMessages.Object, mockApplications.Object, mockJobs.Object);

            //act
            var result = (await viewComponent.InvokeAsync() as ViewViewComponentResult)?.ViewData?.Model as ControlPanelViewModel ?? new();

            //assert
            Assert.That(result.MessagesCount, Is.EqualTo(10));
            Assert.That(result.AplicationsCount, Is.EqualTo(12));
            Assert.That(result.JobsCount, Is.EqualTo(15));
        }
    }
}
