using IPDImexWebsite.Controllers;
using IPDImexWebsite.Models;
using IPDImexWebsite.Models.Repository;
using IPDImexWebsite.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IPDImexWebsiteUnitTests.Controllers
{
    [TestFixture]
    internal class AdministrationControllerTests
    {

        private Mock<ILogger<AdministrationController>> _logger;
        private Mock<IRepositoryMessage> _repository;
        private AdministrationController _administrationController;
        private Mock<IHttpContextAccessor> _accesor;

        [SetUp]
        public void SetUp()
        {
            _logger = new Mock<ILogger<AdministrationController>>();    
            _repository = new Mock<IRepositoryMessage>();   
            _accesor = new Mock<IHttpContextAccessor>();
            _administrationController = new AdministrationController(_repository.Object, _logger.Object, _accesor.Object);
        }

        public async IAsyncEnumerable<Message> MockGetMessages()
        {
            var list = new List<Message>
            {
                    new Message
                    {
                        Id = 1,
                        LastName = "Dragus",
                        PostDateTime = DateTime.Now,
                        ClassificationId = 2,
                        FirstName = "Andreea",
                        Email = "cipri.dragus@yahoo.com",
                        ClientMessage = "Whats up"
                    },
                    new Message
                    {
                        Id = 2,
                        LastName = "Dragus",
                        PostDateTime = DateTime.Now,
                        ClassificationId = 2,
                        FirstName = "Ciprian",
                        Email = "cipri.dragus@yahoo.com",
                        ClientMessage = "Whats up"
                    },
                    new Message
                    {
                        Id = 3,
                        LastName = "Lisca",
                        PostDateTime = DateTime.Now,
                        ClassificationId = 2,
                        FirstName = "Andreea",
                        Email = "cipri.dragus@yahoo.com",
                        ClientMessage = "Whats up"
                    },
                    new Message
                    {
                        Id = 4,
                        LastName = "Petrica",
                        PostDateTime = DateTime.Now,
                        ClassificationId = 2,
                        FirstName = "Mihoc",
                        Email = "cipri.dragus@yahoo.com",
                        ClientMessage = "Whats up"
                    }
            };

            foreach (var message in list)
            {
                yield return message;
            }
            await Task.CompletedTask;

        }

        public async Task<Message?> MockGetMessageByIdAsyncClassificationUnread(int messageId)
        {

            return await Task.FromResult(new Message
            {
                Id = messageId,
                LastName = "Petrica",
                PostDateTime = DateTime.Now,
                Classification = new MessageClassification
                {
                    Id = 2,
                    Name = "Unread"
                },
                ClassificationId = 2,
                FirstName = "Mihoc",
                Email = "cipri.dragus@yahoo.com",
                ClientMessage = "Whats up"
            });

        }
        public async Task<Message?> MockGetMessageByIdAsyncClassificationRead(int messageId)
        {

            return await Task.FromResult(new Message
            {
                Id = messageId,
                LastName = "Petrica",
                PostDateTime = DateTime.Now,
                Classification = new MessageClassification
                {
                    Id = 1,
                    Name = "Read"
                },
                ClassificationId = 2,
                FirstName = "Mihoc",
                Email = "cipri.dragus@yahoo.com",
                ClientMessage = "Whats up"
            });

        }

        #region MessagesPanel Tests
        //MessagesPanel
        [Test]
        public async Task MessagePannel_CanCountReadAndUnreadMessages()
        {
            //arrange
            _repository.Setup(x => x.GetAllMessages()).Returns(MockGetMessages());
            _repository.Setup(x => x.GetAllMessagesCount()).Returns(Task.FromResult(25));
            _repository.Setup(x => x.GetUnreadMessagesCount()).Returns(Task.FromResult(12));

            _administrationController.PageSize = 10;

            //act
            var result = (await _administrationController.MessagesPanel(1) as ViewResult)?.ViewData.Model as MessagesPanelViewModel ?? new();

            //assert
            Assert.That(result.TotalMessages, Is.EqualTo(25));
            Assert.That(result.UnreadMessages, Is.EqualTo(12));
        }

        [Test]
        public async Task MessagePannel_CanLoadMessages()
        {
            //arrange
            _repository.Setup(x => x.GetAllMessages()).Returns(MockGetMessages());
            _repository.Setup(x => x.GetAllMessagesCount()).Returns(Task.FromResult(25));
            _repository.Setup(x => x.GetUnreadMessagesCount()).Returns(Task.FromResult(12));

            _administrationController.PageSize = 10;

            //act
            var result = await _administrationController.MessagesPanel(1) as ViewResult;
            var model = result?.ViewData.Model as MessagesPanelViewModel ?? new();
            var messagesList = model.Messages.ToList();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result?.ViewData.ContainsKey("CurrentUrl"), Is.True);
                Assert.That(messagesList[0].Id, Is.EqualTo(4));
                Assert.That(messagesList[0].LastName, Is.EqualTo("Petrica"));

                Assert.That(messagesList[3].Id, Is.EqualTo(1));
                Assert.That(messagesList[3].LastName, Is.EqualTo("Dragus"));
            });
        }


        [Test]
        public async Task MessagePannel_CanPaginate()
        {
            //arrange
            _repository.Setup(x => x.GetAllMessages()).Returns(MockGetMessages());
            _repository.Setup(x => x.GetAllMessagesCount()).Returns(Task.FromResult(25));
            _repository.Setup(x => x.GetUnreadMessagesCount()).Returns(Task.FromResult(12));

            _administrationController.PageSize = 2;

            //act
            var result = (await _administrationController.MessagesPanel(2) as ViewResult)?.ViewData.Model as MessagesPanelViewModel ?? new();
            var messagesList = result.Messages.ToList();
            var pagination = result.Pagination;

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(pagination.CurrentPage, Is.EqualTo(2));
                Assert.That(pagination.ItemsPerPage, Is.EqualTo(2));
                Assert.That(pagination.TotalPages, Is.EqualTo(2));

                Assert.That(messagesList[0].Id, Is.EqualTo(2));
                Assert.That(messagesList[0].FirstName, Is.EqualTo("Ciprian"));
                Assert.That(messagesList[1].Id, Is.EqualTo(1));
                Assert.That(messagesList[1].FirstName, Is.EqualTo("Andreea"));
            });
        }
        #endregion

        #region Message Tests        
        [Test]
        public async Task Message_LoadMessageButIsNull_ReturnAdminInfoPage()
        {
            //arrange
            _repository.Setup(c => c.GetMessageByIdAsync(It.IsAny<int>())).Returns(async (int messageId) => await Task.FromResult(default(Message)));

            //act
            var result = await _administrationController.Message(2) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/AdminInfo"));
        }

        [Test]
        public async Task Message_CanLoadTheMessage_ReturnMessageView()
        {
            //arrange
            _repository.Setup(c => c.GetMessageByIdAsync(It.IsAny<int>())).Returns(MockGetMessageByIdAsyncClassificationUnread(2));

            //act
            var result = (await _administrationController.Message(2) as ViewResult)?.ViewData.Model as Message ?? new();

            //assert
            Assert.That(result?.Id, Is.EqualTo(2));
            Assert.That(result?.LastName, Is.EqualTo("Petrica"));
            Assert.That(result?.FirstName, Is.EqualTo("Mihoc"));
        }

        [Test]
        public async Task Message_CanLoadTheMessageAndChangeClassificationToRead_ReturnMessageView()
        {
            //arrange
            _repository.Setup(c => c.GetMessageByIdAsync(It.IsAny<int>())).Returns(MockGetMessageByIdAsyncClassificationUnread(2));
            _repository.Setup(c => c.MarksAsReadAsync(It.IsAny<Message>())).Returns(async () => await Task.FromResult(true));

            //act
            var result = (await _administrationController.Message(2) as ViewResult)?.ViewData.Model as Message ?? new();

            //assert
            _repository.Verify(x => x.MarksAsReadAsync(It.IsAny<Message>()), Times.Once);
        }
        [Test]
        public async Task Message_CanLoadTheMessageButClassificationIsReadSoItWontCallMarkAsRead_ReturnMessageView()
        {
            //arrange
            _repository.Setup(c => c.GetMessageByIdAsync(It.IsAny<int>())).Returns(MockGetMessageByIdAsyncClassificationRead(2));

            //act
            var result = (await _administrationController.Message(2) as ViewResult)?.ViewData.Model as Message ?? new();

            //assert
            _repository.Verify(x => x.MarksAsReadAsync(It.IsAny<Message>()), Times.Never);
        }
        #endregion

        #region DeleteMessages Tests
        [Test]
        public async Task DeleteMessage_MessageWasDeleted_RedirectToMessagePanel()
        {
            //arrange
            _repository.Setup(x => x.DeleteMessageAsync(It.IsAny<int>())).Returns(async () => await Task.FromResult(true));
          
            //act
            var result = await _administrationController.DeleteMessage(1) as RedirectToActionResult;

            //assert
            Assert.That(result!.ActionName, Is.EqualTo("MessagesPanel"));
        }
        [Test]
        public async Task DeleteMessage_MessageWasNotDeleted_RedirectToAdminInfo()
        {
            //arrange
            _repository.Setup(x => x.DeleteMessageAsync(It.IsAny<int>())).Returns(async () => await Task.FromResult(false));
         
            //act
            var result = await _administrationController.DeleteMessage(1) as RedirectToPageResult;

            //assert
            Assert.That(result!.PageName, Is.EqualTo("/AdminInfo"));
        }
        #endregion

        #region SearchMessages Tests
        [Test]
        [TestCase(null)]
        [TestCase("")]
        public async Task SearchMessages_SearchCriteriaIsNullOrEmptytReturnMessagesPanel(string searchCriteria)
        {

            //act
            var result = await _administrationController.SearchMessages(searchCriteria, 1) as RedirectToActionResult;

            //assert
            Assert.That(result!.ActionName, Is.EqualTo("MessagesPanel"));
        }

        [Test]
        public async Task SearchMessages_CanCountReadAndUnreadMessages()
        {
            //arrange
            _repository.Setup(x => x.SearchAsync(It.IsAny<string>())).Returns(MockGetMessages());
            _repository.Setup(x => x.GetAllMessagesCount()).Returns(Task.FromResult(25));
            _repository.Setup(x => x.GetUnreadMessagesCount()).Returns(Task.FromResult(12));

            _administrationController.PageSize = 10;

            //act
            var result = (await _administrationController.SearchMessages("Test", 1) as ViewResult)?.ViewData.Model as MessagesPanelViewModel ?? new();

            //assert
            Assert.That(result.TotalMessages, Is.EqualTo(25));
            Assert.That(result.UnreadMessages, Is.EqualTo(12));
        }
        [Test]
        public async Task SearchMessages_CanSearchThroughMessages()
        {
            //arrange
            _repository.Setup(x => x.SearchAsync(It.IsAny<string>())).Returns(MockGetMessages());
            _repository.Setup(x => x.GetAllMessagesCount()).Returns(Task.FromResult(25));
            _repository.Setup(x => x.GetUnreadMessagesCount()).Returns(Task.FromResult(12));

            _administrationController.PageSize = 10;

            //act
            var result = (await _administrationController.SearchMessages("Test", 1) as ViewResult)?.ViewData.Model as MessagesPanelViewModel ?? new();
            var messagesList = result.Messages.ToList();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(messagesList[0].Id, Is.EqualTo(4));
                Assert.That(messagesList[0].LastName, Is.EqualTo("Petrica"));

                Assert.That(messagesList[3].Id, Is.EqualTo(1));
                Assert.That(messagesList[3].LastName, Is.EqualTo("Dragus"));
            });
        }
        [Test]
        public async Task SearchMessages_CanPaginate()
        {
            //arrange
            _repository.Setup(x => x.SearchAsync(It.IsAny<string>())).Returns(MockGetMessages());
            _repository.Setup(x => x.GetAllMessagesCount()).Returns(Task.FromResult(25));
            _repository.Setup(x => x.GetUnreadMessagesCount()).Returns(Task.FromResult(12));

            _administrationController.PageSize = 2;

            //act
            var result = (await _administrationController.SearchMessages("Test", 2) as ViewResult)?.ViewData.Model as MessagesPanelViewModel ?? new();
            var messagesList = result.Messages.ToList();
            var pagination = result.Pagination;

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(pagination.CurrentPage, Is.EqualTo(2));
                Assert.That(pagination.ItemsPerPage, Is.EqualTo(2));
                Assert.That(pagination.TotalPages, Is.EqualTo(2));

                Assert.That(messagesList[0].Id, Is.EqualTo(2));
                Assert.That(messagesList[0].FirstName, Is.EqualTo("Ciprian"));
                Assert.That(messagesList[1].Id, Is.EqualTo(1));
                Assert.That(messagesList[1].FirstName, Is.EqualTo("Andreea"));
            });
        }
        [Test]
        public async Task SearchMessages_CanSendSearchRequestAndCreateViewDataCurrentUrl()
        {
            //arrange
            _repository.Setup(x => x.SearchAsync(It.IsAny<string>())).Returns(MockGetMessages());
            _repository.Setup(x => x.GetAllMessagesCount()).Returns(Task.FromResult(25));
            _repository.Setup(x => x.GetUnreadMessagesCount()).Returns(Task.FromResult(12));

            _administrationController.PageSize = 2;

            //act
            var result = await _administrationController.SearchMessages("Test", 2) as ViewResult;
            var model = result?.ViewData?.Model as MessagesPanelViewModel ?? new();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result?.ViewData.ContainsKey("CurrentUrl"), Is.True);
                Assert.That(_administrationController.ViewBag.SearchCriteria, Is.Not.Null);
                Assert.That(model.SearchRequest, Is.EqualTo(true));
                Assert.That(model.SearchCriteria, Is.EqualTo("Test"));
            });
        }

        #endregion
    }
}
