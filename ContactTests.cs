using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public void OnPost_ModelStateIsNotValid_ReturnPage()
        {
            //arrange
            var mockRepository = new Mock<IRepositoryMessage>();
            ContactModel model  = new ContactModel(mockRepository.Object);
            model.ModelState.AddModelError("error", "customError");

            //act 
            var result = model.OnPost() as PageResult;

            //assert
            mockRepository.Verify(x => x.SendMessage(It.IsAny<Message>()), Times.Never);
        }
        [Test]
        public void OnPost_ModelStateIsValid_ReturnPage()
        {
            //arrange
            var mockRepository = new Mock<IRepositoryMessage>();
            ContactModel model = new ContactModel(mockRepository.Object);

            //act
            var result = model.OnPost() as RedirectToPageResult;

            //assert
            mockRepository.Verify(x => x.SendMessage(It.IsAny<Message>()), Times.Once);
            Assert.That(result?.PageName, Is.EqualTo("/ClientInfo"));
        }

    }
}
