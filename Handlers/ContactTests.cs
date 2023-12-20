using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using IPDImexWebsite.CustomServices;
using IPDImexWebsite.Models;
using IPDImexWebsite.Models.Repository;
using IPDImexWebsite.Pages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace IPDImexWebsiteUnitTests
{
    [TestFixture]
    internal class ContactTests
    {
        private Mock<ILogger<ContactModel>> _logger;
        private Mock<IRepositoryMessage> _repMessage;
        private Mock<IEmailService> _emailService;
        private Mock<IFileWrapper> _fileWrapper;
        private Mock<IWebHostEnvironment> _webHost;
        private ContactModel _model;

        [SetUp]
        public void SetUp()
        {
            _logger = new Mock<ILogger<ContactModel>>();
            _repMessage = new Mock<IRepositoryMessage>();
            _emailService = new Mock<IEmailService>();
            _fileWrapper = new Mock<IFileWrapper>();
            _webHost = new Mock<IWebHostEnvironment>();

            _model = new ContactModel(_repMessage.Object, _emailService.Object, _logger.Object, _fileWrapper.Object, _webHost.Object);
        }

        [Test]
        public async Task OnPost_ModelStateIsNotValid_ReturnPage()
        {
            //arrange
            _model.ModelState.AddModelError("error", "customError");

            //act 
            var result = await _model.OnPost() as PageResult;

            //assert
            _repMessage.Verify(x => x.SendMessage(It.IsAny<Message>()), Times.Never);
            _emailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
        [Test]
        public async Task OnPost_WorksSmothly_ReturnPage()
        {
            //act
            var result = await _model.OnPost() as RedirectToPageResult;

            //assert
            _repMessage.Verify(x => x.SendMessage(It.IsAny<Message>()), Times.Once);
            _emailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.AtLeast(2));
            Assert.That(result?.PageName, Is.EqualTo("/ClientInfo"));
        }

        [Test]
        public async Task OnPost_SendEmailThrowsExceptionResponseIsNotBroken_ReturnPage()
        {
            //arrange
            _emailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Throws(new Exception());

            //act
            var result = await _model.OnPost() as RedirectToPageResult;

            //assert
            _repMessage.Verify(x => x.SendMessage(It.IsAny<Message>()), Times.Once());
            _emailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _fileWrapper.Verify(x => x.ReadAllTextAsync(It.IsAny<string>()), Times.Once());
            Assert.That(result?.PageName, Is.EqualTo("/ClientInfo"));
        }
    }
}
