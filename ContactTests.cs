using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IPDImexWebsite.CustomServices;
using IPDImexWebsite.Models;
using IPDImexWebsite.Models.Repository;
using IPDImexWebsite.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

namespace IPDImexWebsiteUnitTests
{
    [TestFixture]
    internal class ContactTests
    {
        [Test]
        public async Task OnPost_ModelStateIsNotValid_ReturnPage()
        {
            //arrange
            var mockRepository = new Mock<IRepositoryMessage>();
            var mockEmail = new Mock<IEmailService>();
            ContactModel model  = new ContactModel(mockRepository.Object,mockEmail.Object);
            model.ModelState.AddModelError("error", "customError");

            //act 
            var result = await model.OnPost() as PageResult;

            //assert
            mockRepository.Verify(x => x.SendMessage(It.IsAny<Message>()), Times.Never);
            mockEmail.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
        [Test]
        public async Task OnPost_ModelStateIsValid_ReturnPage()
        {
            //arrange
            var mockRepository = new Mock<IRepositoryMessage>();
            var mockEmail = new Mock<IEmailService>();
            ContactModel model = new ContactModel(mockRepository.Object, mockEmail.Object);

            //act
            var result = await model.OnPost() as RedirectToPageResult;

            //assert
            mockRepository.Verify(x => x.SendMessage(It.IsAny<Message>()), Times.Once);
            mockEmail.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            Assert.That(result?.PageName, Is.EqualTo("/ClientInfo"));
        }

        [Test]
        public async Task OnPost_SendEmailThrowsException_ReturnPage()
        {
            //arrange
            var mockRepository = new Mock<IRepositoryMessage>();
            var mockEmail = new Mock<IEmailService>();
            mockEmail.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Throws(new Exception());
            ContactModel model = new ContactModel(mockRepository.Object, mockEmail.Object);

            //act
            var result = await model.OnPost() as RedirectToPageResult;

            //assert
            mockRepository.Verify(x => x.SendMessage(It.IsAny<Message>()), Times.Once);
            mockEmail.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            Assert.That(result?.PageName, Is.EqualTo("/ClientInfo"));
            Assert.ThrowsAsync<Exception>(async () => await mockEmail.Object.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
        }
        [Test]
        public async Task OnPost_SendMessageThrowsException_ReturnPage()
        {
            //arrange
            var mockRepository = new Mock<IRepositoryMessage>();
            mockRepository.Setup(x => x.SendMessage(It.IsAny<Message>())).Throws(new Exception("A aparut o eroare!"));

            var mockEmail = new Mock<IEmailService>();
            ContactModel model = new ContactModel(mockRepository.Object, mockEmail.Object);

            //act
            var result = await model.OnPost() as RedirectToPageResult;

            //assert
            mockRepository.Verify(x => x.SendMessage(It.IsAny<Message>()), Times.Once);
            mockEmail.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.That(result?.PageName, Is.EqualTo("/ClientInfo"));
            Assert.That(result?.RouteValues?["info"], Is.EqualTo("Ceva nu a mers bine , încearcă dinou sau contactează dezvoltatorul!"));
            Assert.ThrowsAsync<Exception>(async () => await mockRepository.Object.SendMessage(It.IsAny<Message>()));
        }

    }
}
