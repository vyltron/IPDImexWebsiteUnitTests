using IPDImexWebsite.Controllers;
using IPDImexWebsite.Models;
using IPDImexWebsite.Models.Repository;
using IPDImexWebsite.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IPDImexWebsiteUnitTests
{
    [TestFixture]
    internal class AdministrationControllerTests
    {
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
            var mockMessages = new Mock<IRepositoryMessage>();
            var mockLogger = new Mock<ILogger<AdministrationController>>();
            mockMessages.Setup(x => x.GetAllMessages()).Returns(MockGetMessages());
            mockMessages.Setup(x => x.GetAllMessagesCount()).Returns(Task.FromResult(25));
            mockMessages.Setup(x => x.GetUnreadMessagesCount()).Returns(Task.FromResult(12));

            var controller = new AdministrationController(mockMessages.Object, mockLogger.Object);
            controller.PageSize = 10;

            //act
            var result = (await controller.MessagesPanel(1) as ViewResult)?.ViewData.Model as MessagesPanelViewModel ?? new();

            //assert
            Assert.That(result.TotalMessages, Is.EqualTo(25));
            Assert.That(result.UnreadMessages, Is.EqualTo(12));
        }

        [Test]
        public async Task MessagePannel_CanLoadMessages()
        {
            //arrange
            var mockMessages = new Mock<IRepositoryMessage>();
            var mockLogger = new Mock<ILogger<AdministrationController>>();
            mockMessages.Setup(x => x.GetAllMessages()).Returns(MockGetMessages());
            mockMessages.Setup(x => x.GetAllMessagesCount()).Returns(Task.FromResult(25));
            mockMessages.Setup(x => x.GetUnreadMessagesCount()).Returns(Task.FromResult(12));

            var controller = new AdministrationController(mockMessages.Object, mockLogger.Object);
            controller.PageSize = 10;

            //act
            var result = (await controller.MessagesPanel(1) as ViewResult)?.ViewData.Model as MessagesPanelViewModel ?? new();
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
        public async Task MessagePannel_CanPaginate()
        {
            //arrange
            var mockMessages = new Mock<IRepositoryMessage>();
            var mockLogger = new Mock<ILogger<AdministrationController>>();
            mockMessages.Setup(x => x.GetAllMessages()).Returns(MockGetMessages());
            mockMessages.Setup(x => x.GetAllMessagesCount()).Returns(Task.FromResult(25));
            mockMessages.Setup(x => x.GetUnreadMessagesCount()).Returns(Task.FromResult(12));

            var controller = new AdministrationController(mockMessages.Object, mockLogger.Object);
            controller.PageSize = 2;

            //act
            var result = (await controller.MessagesPanel(2) as ViewResult)?.ViewData.Model as MessagesPanelViewModel ?? new();
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
        public async Task MessagePannel_ThrowsError()
        {
            //arrange
            var mockMessages = new Mock<IRepositoryMessage>();
            var mockLogger = new Mock<ILogger<AdministrationController>>();
            mockMessages.Setup(x => x.GetAllMessages()).Returns(MockGetMessages());
            mockMessages.Setup(x => x.GetAllMessagesCount()).Throws(new Exception("Eroare"));
            mockMessages.Setup(x => x.GetUnreadMessagesCount()).Returns(Task.FromResult(12));

            var controller = new AdministrationController(mockMessages.Object, mockLogger.Object);
            controller.PageSize = 2;

            //act
            var result = await controller.MessagesPanel(2) as RedirectToPageResult;

            //assert
            mockMessages.Verify(x => x.GetUnreadMessagesCount(), Times.Never());
            mockMessages.Verify(x => x.GetAllMessages(), Times.Once());
            Assert.ThrowsAsync<Exception>(async () => await mockMessages.Object.GetAllMessagesCount());
            Assert.That(result!.PageName, Is.EqualTo("/AdminInfo"));
        }
        #endregion

        #region Message Tests        
        [Test]
        public async Task Message_LoadMessageButIsNull_ReturnAdminInfoPage()
        {
            //arrange
            var mockMessage = new Mock<IRepositoryMessage>();
            mockMessage.Setup(c => c.GetMessageByIdAsync(It.IsAny<int>())).Returns( async (int messageId) =>  await Task.FromResult(default(Message)));
            var mockLogger = new Mock<ILogger<AdministrationController>>();

            var controller = new AdministrationController(mockMessage.Object, mockLogger.Object);

            //act
           var result = await controller.Message(2) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/AdminInfo"));
        }
      
        [Test]
        public async Task Message_CanLoadTheMessage_ReturnMessageView()
        {
            //arrange
            var mockMessage = new Mock<IRepositoryMessage>();
            mockMessage.Setup(c => c.GetMessageByIdAsync(It.IsAny<int>())).Returns(MockGetMessageByIdAsyncClassificationUnread(2));
            var mockLogger = new Mock<ILogger<AdministrationController>>();

            var controller = new AdministrationController(mockMessage.Object, mockLogger.Object);

            //act
            var result = (await controller.Message(2) as ViewResult)?.ViewData.Model as Message ?? new();

            //assert
            Assert.That(result?.Id, Is.EqualTo(2));
            Assert.That(result?.LastName, Is.EqualTo("Petrica"));
            Assert.That(result?.FirstName, Is.EqualTo("Mihoc"));
        }

        [Test]
        public async Task Message_CanLoadTheMessageAndChangeClassificationToRead_ReturnMessageView()
        {
            //arrange
            var mockMessage = new Mock<IRepositoryMessage>();
            var mockLogger = new Mock<ILogger<AdministrationController>>();
            mockMessage.Setup(c => c.GetMessageByIdAsync(It.IsAny<int>())).Returns(MockGetMessageByIdAsyncClassificationUnread(2));
            mockMessage.Setup(c => c.MarksAsReadAsync(It.IsAny<Message>())).Returns(async () => await Task.FromResult(true));

            var controller = new AdministrationController(mockMessage.Object, mockLogger.Object);

            //act
            var result = (await controller.Message(2) as ViewResult)?.ViewData.Model as Message ?? new();

            //assert
            mockMessage.Verify(x => x.MarksAsReadAsync(It.IsAny<Message>()), Times.Once);
        }
        [Test]
        public async Task Message_CanLoadTheMessageButClassificationIsReadSoItWontCallMarkAsRead_ReturnMessageView()
        {
            //arrange
            var mockMessage = new Mock<IRepositoryMessage>();
            mockMessage.Setup(c => c.GetMessageByIdAsync(It.IsAny<int>())).Returns(MockGetMessageByIdAsyncClassificationRead(2));
            var mockLogger = new Mock<ILogger<AdministrationController>>();

            var controller = new AdministrationController(mockMessage.Object, mockLogger.Object);

            //act
            var result = (await controller.Message(2) as ViewResult)?.ViewData.Model as Message ?? new();

            //assert
            mockMessage.Verify(x => x.MarksAsReadAsync(It.IsAny<Message>()), Times.Never);
        }

        [Test]
        public async Task Message_ThrowsError_ReturnMessageView()
        {
            //arrange
            var mockMessage = new Mock<IRepositoryMessage>();
            mockMessage.Setup(c => c.GetMessageByIdAsync(It.IsAny<int>())).Throws(new Exception("eroare"));
            var mockLogger = new Mock<ILogger<AdministrationController>>();

            var controller = new AdministrationController(mockMessage.Object, mockLogger.Object);

            //act
            var result = await controller.Message(2) as RedirectToPageResult;

            //assert
            mockMessage.Verify(x => x.MarksAsReadAsync(It.IsAny<Message>()), Times.Never);
            mockMessage.Verify(x => x.GetMessageByIdAsync(It.IsAny<int>()), Times.Once);
            Assert.That(result!.PageName, Is.EqualTo("/AdminInfo"));
            Assert.ThrowsAsync<Exception>(async () => await mockMessage.Object.GetMessageByIdAsync(2));
        }
        #endregion

        #region DeleteMessages Tests
        [Test]
        public async Task DeleteMessage_MessageWasDeleted_RedirectToMessagePanel()
        {
            //arrange
            var mock = new Mock<IRepositoryMessage>();
            mock.Setup(x => x.DeleteMessageAsync(It.IsAny<int>())).Returns(async () => await Task.FromResult(true));
            var mockLogger = new Mock<ILogger<AdministrationController>>();
            //act
            var controller = new AdministrationController(mock.Object, mockLogger.Object);
            var result = await controller.DeleteMessage(1) as RedirectToActionResult;

            //assert
            Assert.That(result!.ActionName, Is.EqualTo("MessagesPanel"));
        }
        [Test]
        public async Task DeleteMessage_MessageWasNotDeleted_RedirectToAdminInfo()
        {
            //arrange
            var mock = new Mock<IRepositoryMessage>();
            mock.Setup(x => x.DeleteMessageAsync(It.IsAny<int>())).Returns(async () => await Task.FromResult(false));
            var mockLogger = new Mock<ILogger<AdministrationController>>();
            //act
            var controller = new AdministrationController(mock.Object, mockLogger.Object);
            var result = await controller.DeleteMessage(1) as RedirectToPageResult;

            //assert
            Assert.That(result!.PageName, Is.EqualTo("/AdminInfo"));
        }

        [Test]
        public async Task DeleteMessage_THrowsError_RedirectToAdminInfo()
        {
            //arrange
            var mock = new Mock<IRepositoryMessage>();
            mock.Setup(x => x.DeleteMessageAsync(It.IsAny<int>())).Throws(new Exception("eroare"));
            var mockLogger = new Mock<ILogger<AdministrationController>>();
            //act
            var controller = new AdministrationController(mock.Object, mockLogger.Object);
            var result = await controller.DeleteMessage(1) as RedirectToPageResult;

            //assert

            Assert.That(result!.PageName, Is.EqualTo("/AdminInfo"));
            mock.Verify(x => x.DeleteMessageAsync(It.IsAny<int>()), Times.Once);
            Assert.ThrowsAsync<Exception>(async () => await mock.Object.DeleteMessageAsync(It.IsAny<int>()));
        }
        #endregion

        #region SearchMessages Tests
        [Test]
        [TestCase(null)]
        [TestCase("")]
        public async Task SearchMessages_SearchCriteriaIsNullOrEmptytReturnMessagesPanel(string searchCriteria)
        {
            //assert
            var mock = new Mock<IRepositoryMessage>();
            var mockLogger = new Mock<ILogger<AdministrationController>>();
            var controller = new AdministrationController(mock.Object, mockLogger.Object);

            //act
           var result = await controller.SearchMessages(searchCriteria, 1) as RedirectToActionResult;

            //assert
            Assert.That(result!.ActionName, Is.EqualTo("MessagesPanel"));
        }

        [Test]
        public async Task SearchMessages_CanCountReadAndUnreadMessages()
        {
            //arrange
            var mockMessages = new Mock<IRepositoryMessage>();
            mockMessages.Setup(x => x.SearchAsync(It.IsAny<string>())).Returns(MockGetMessages());
            mockMessages.Setup(x => x.GetAllMessagesCount()).Returns(Task.FromResult(25));
            mockMessages.Setup(x => x.GetUnreadMessagesCount()).Returns(Task.FromResult(12));
            var mockLogger = new Mock<ILogger<AdministrationController>>();

            var controller = new AdministrationController(mockMessages.Object, mockLogger.Object);
            controller.PageSize = 10;

            //act
            var result = (await controller.SearchMessages("Test",1) as ViewResult)?.ViewData.Model as MessagesPanelViewModel ?? new();

            //assert
            Assert.That(result.TotalMessages, Is.EqualTo(25));
            Assert.That(result.UnreadMessages, Is.EqualTo(12));
        }
        [Test]
        public async Task SearchMessages_CanSearchThroughMessages()
        {
            //arrange
            var mockMessages = new Mock<IRepositoryMessage>();
            mockMessages.Setup(x => x.SearchAsync(It.IsAny<string>())).Returns(MockGetMessages());
            mockMessages.Setup(x => x.GetAllMessagesCount()).Returns(Task.FromResult(25));
            mockMessages.Setup(x => x.GetUnreadMessagesCount()).Returns(Task.FromResult(12));
            var mockLogger = new Mock<ILogger<AdministrationController>>();

            var controller = new AdministrationController(mockMessages.Object, mockLogger.Object);
            controller.PageSize = 10;

            //act
            var result = (await controller.SearchMessages("Test",1) as ViewResult)?.ViewData.Model as MessagesPanelViewModel ?? new();
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
            var mockMessages = new Mock<IRepositoryMessage>();
            mockMessages.Setup(x => x.SearchAsync(It.IsAny<string>())).Returns(MockGetMessages());
            mockMessages.Setup(x => x.GetAllMessagesCount()).Returns(Task.FromResult(25));
            mockMessages.Setup(x => x.GetUnreadMessagesCount()).Returns(Task.FromResult(12));
            var mockLogger = new Mock<ILogger<AdministrationController>>();

            var controller = new AdministrationController(mockMessages.Object, mockLogger.Object);
            controller.PageSize = 2;

            //act
            var result = (await controller.SearchMessages("Test",2) as ViewResult)?.ViewData.Model as MessagesPanelViewModel ?? new();
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
        public async Task SearchMessages_CanSendSearchRequest()
        {
            //arrange
            var mockMessages = new Mock<IRepositoryMessage>();
            mockMessages.Setup(x => x.SearchAsync(It.IsAny<string>())).Returns(MockGetMessages());
            mockMessages.Setup(x => x.GetAllMessagesCount()).Returns(Task.FromResult(25));
            mockMessages.Setup(x => x.GetUnreadMessagesCount()).Returns(Task.FromResult(12));
            var mockLogger = new Mock<ILogger<AdministrationController>>();

            var controller = new AdministrationController(mockMessages.Object, mockLogger.Object);
            controller.PageSize = 2;

            //act
            var result = (await controller.SearchMessages("Test", 2) as ViewResult)?.ViewData.Model as MessagesPanelViewModel ?? new();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result.SearchRequest, Is.EqualTo(true));
                Assert.That(result.SearchCriteria, Is.EqualTo("Test"));
            });

        }
        [Test]
        public async Task SearchMessages_ThrowsError()
        {
            //arrange
            var mockMessages = new Mock<IRepositoryMessage>();
            mockMessages.Setup(x => x.SearchAsync(It.IsAny<string>())).Returns(MockGetMessages());
            mockMessages.Setup(x => x.GetAllMessagesCount()).Throws(new Exception("Eroare"));
            mockMessages.Setup(x => x.GetUnreadMessagesCount()).Returns(Task.FromResult(12));
            var mockLogger = new Mock<ILogger<AdministrationController>>();

            var controller = new AdministrationController(mockMessages.Object, mockLogger.Object);
            controller.PageSize = 2;

            //act
            var result = await controller.SearchMessages("Test", 2) as RedirectToPageResult;

            //assert
            mockMessages.Verify(x => x.GetAllMessagesCount(), Times.Once);
            mockMessages.Verify(x => x.GetUnreadMessagesCount(), Times.Never);
            Assert.That(result!.PageName, Is.EqualTo("/AdminInfo"));
            Assert.ThrowsAsync<Exception>(async () => await mockMessages.Object.GetAllMessagesCount());
        }

        #endregion
    }
}
