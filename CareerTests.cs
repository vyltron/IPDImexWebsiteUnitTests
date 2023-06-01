using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using IPDImexWebsite.Controllers;
using IPDImexWebsite.Models;
using IPDImexWebsite.Models.Repository;
using Microsoft.AspNetCore.Mvc;
using Moq;
using IPDImexWebsite.ViewModels;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using IPDImexWebsite.CustomServices;
using System.Security.Cryptography.X509Certificates;
using Castle.Core.Logging;
using Microsoft.Extensions.Logging;

namespace IPDImexWebsiteUnitTests
{
    [TestFixture]
    internal class CareerTests
    {
        public async IAsyncEnumerable<Job> MockGetJobs()
        {
            var list = new List<Job>
            {
                new Job
                {
                    Id= 1,
                    JobName = "Job1"
                },
                new Job
                {
                    Id= 2,
                    JobName = "Job2"
                },
                new Job
                {
                    Id= 3,
                    JobName = "Job3"
                }
            };

            foreach (var job in list)
            {
                yield return job;
            }
            await Task.CompletedTask;

        }
        [Test]
        public async Task Index_CanGetJobs_ReturnView()
        {
            //arrange
            var mockRepAplication = new Mock<IRepositoryAplication>();
            var mockEmail = new Mock<IEmailService>();

            var mockRepJob = new Mock<IRepositoryJob>();
            mockRepJob.Setup(x => x.GetJobsAsync()).Returns(MockGetJobs());

            var mockLogger = new Mock<ILogger<CareersController>>();

            var controller = new CareersController(mockRepAplication.Object, mockRepJob.Object, mockEmail.Object, mockLogger.Object);

            //act 
            var results = (await controller.Index() as ViewResult)?.ViewData.Model as CarrersViewModel ?? new();
            var jobs = results.Jobs.ToList();
            //assert
            Assert.Multiple(() => {
                Assert.That(jobs, Has.Count.EqualTo(3));
                Assert.That(jobs[0].Id, Is.EqualTo(1));
                Assert.That(jobs[0].JobName, Is.EqualTo("Job1"));
                Assert.That(jobs[2].Id, Is.EqualTo(3));
                Assert.That(jobs[2].JobName, Is.EqualTo("Job3"));
            });
        }
        [Test]
        public async Task Index_SoemthingWentWrong_ThrowExceptionReturnViewWithEmptyJobs()
        {
            //arrange
            var mockRepAplication = new Mock<IRepositoryAplication>();
            var mockEmail = new Mock<IEmailService>();
            
            var mockLogger = new Mock<ILogger<CareersController>>();

            var mockRepJob = new Mock<IRepositoryJob>();
            mockRepJob.Setup(x => x.GetJobsAsync()).Throws(new Exception("A aparut o eroare!"));

            var controller = new CareersController(mockRepAplication.Object, mockRepJob.Object, mockEmail.Object, mockLogger.Object);

            //act 
            var results = (await controller.Index() as ViewResult)?.ViewData.Model as CarrersViewModel ?? new();
            //assert

            Assert.That(results.Jobs.Count(), Is.EqualTo(0));
            Assert.ThrowsAsync<Exception>(async () =>
            {
                await foreach (var job in mockRepJob.Object.GetJobsAsync())
                {
                    //...
                }
            });
        }

        [Test] 
        public async Task PostAplication_ModelStateIsNotValid_ReturnIndexView()
        {
            //arrange
            var mockRepAplication = new Mock<IRepositoryAplication>();
            var mockEmail = new Mock<IEmailService>();

            var mockRepJob = new Mock<IRepositoryJob>();
            mockRepJob.Setup(x => x.GetJobsAsync()).Returns(MockGetJobs());

            var mockLogger = new Mock<ILogger<CareersController>>();

            var controller = new CareersController(mockRepAplication.Object, mockRepJob.Object, mockEmail.Object, mockLogger.Object);
            controller.ModelState.AddModelError("error", "Test error");

            var viewModel = new CarrersViewModel();

            //act 
            var result = await controller.PostAplication(viewModel) as ViewResult ?? new();


            //assert
            Assert.That(result.ViewData.ModelState.ContainsKey("error"), Is.EqualTo(true));
            Assert.That(result!.ViewName, Is.EqualTo("Index"));
        }
        
        [Test]
        public async Task PostAplication_FormFileIsNull_ReturnIndexView()
        {
            //arrange
            var mockRepAplication = new Mock<IRepositoryAplication>();
            var mockEmail = new Mock<IEmailService>();

            var mockRepJob = new Mock<IRepositoryJob>();
            mockRepJob.Setup(x => x.GetJobsAsync()).Returns(MockGetJobs());

            var mockLogger = new Mock<ILogger<CareersController>>();

            var controller = new CareersController(mockRepAplication.Object, mockRepJob.Object, mockEmail.Object, mockLogger.Object);


            var viewModel = new CarrersViewModel
            {
                Aplication = new Aplication
                {
                    LastName = "Dragus",
                    FirstName = "Ciprian",
                    Email = "cipri.dragus@yaoo.com",
                    Phone = "0747343056",
                    Age = 28,
                    CoverLetter = "Salut",
                    CV = new CV { Id = 1 }
                }
            };

            //act 
            var result = await controller.PostAplication(viewModel) as ViewResult ?? new();

            //assert
            Assert.That(result!.ViewName, Is.EqualTo("Index"));
            Assert.That(result.ViewData.ModelState.ContainsKey("MissingCV"), Is.EqualTo(true));
        }

        [Test]
        public async Task PostAplication_FormFileIsNotNullButIsBiggerThan3mb_ReturnIndexView()
        {
            //arrange
            var mockRepAplication = new Mock<IRepositoryAplication>();
            var mockEmail = new Mock<IEmailService>();

            var mockRepJob = new Mock<IRepositoryJob>();
            mockRepJob.Setup(x => x.GetJobsAsync()).Returns(MockGetJobs());

            //mock  IFormFile
            var content = "This is my cv";
            var fileName = "test.pdf";
            var lengthDouble = 3.5 * 1024 * 1024;
            long lengthLong = (long)lengthDouble;
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;
            IFormFile formFile = new FormFile(stream, 0, lengthLong, "id_from_form", fileName);

            var mockLogger = new Mock<ILogger<CareersController>>();

            var controller = new CareersController(mockRepAplication.Object, mockRepJob.Object, mockEmail.Object, mockLogger.Object);


            var viewModel = new CarrersViewModel
            {
                Aplication = new Aplication
                {
                    LastName = "Dragus",
                    FirstName = "Ciprian",
                    Email = "cipri.dragus@yaoo.com",
                    Phone = "0747343056",
                    Age = 28,
                    CoverLetter = "Salut",
                    CV = new CV { Id = 1 }
                },
                FormFile = formFile,
            };

            //act 
            var result = await controller.PostAplication(viewModel) as ViewResult ?? new();
            
            var maxFileSize = 3 * 1024 * 1024;//set 3mb <  3.5mb

            //assert
            Assert.That(viewModel.FormFile.Length, Is.GreaterThan(maxFileSize));//perform the same operation as in the method.
            Assert.That(result!.ViewName, Is.EqualTo("Index"));
            Assert.That(result.ViewData.ModelState.ContainsKey("BigFile"), Is.EqualTo(true));
        }
      

        [Test]
        public async Task PostAplication_FormFileIsNotPDF_ReturnIndexView()
        {
            //arrange
            var mockRepAplication = new Mock<IRepositoryAplication>();

            var mockRepJob = new Mock<IRepositoryJob>();
            mockRepJob.Setup(x => x.GetJobsAsync()).Returns(MockGetJobs());
            var mockEmail = new Mock<IEmailService>();

            //mock  IFormFile
            var content = "This is my cv";
            var fileName = "test.txt";//set type to txt
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;
            IFormFile formFile = new FormFile(stream, 0, stream.Length, "id_from_form", fileName) 
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/plain"
            };

            var mockLogger = new Mock<ILogger<CareersController>>();

            var controller = new CareersController(mockRepAplication.Object, mockRepJob.Object, mockEmail.Object, mockLogger.Object);


            var viewModel = new CarrersViewModel
            {
                Aplication = new Aplication
                {
                    LastName = "Dragus",
                    FirstName = "Ciprian",
                    Email = "cipri.dragus@yaoo.com",
                    Phone = "0747343056",
                    Age = 28,
                    CoverLetter = "Salut",
                    CV = new CV { Id = 1 }
                },
                FormFile = formFile,
            };

            //act 
            var result = await controller.PostAplication(viewModel) as ViewResult ?? new();

            //assert
            Assert.That(formFile.ContentType, Is.EqualTo("text/plain"));//check the opposite as in the method
            Assert.That(result!.ViewName, Is.EqualTo("Index"));
            Assert.That(result.ViewData.ModelState.ContainsKey("NotPDFType"), Is.EqualTo(true));
        }

        [Test]
        public async Task PostAplication_EverythingWorks_ReturnClientInfoRazorPage()
        {
            //arrange
            var mockRepAplication = new Mock<IRepositoryAplication>();
            var mockEmail = new Mock<IEmailService>();
            mockEmail.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(async () => await Task.FromResult(Task.CompletedTask));

            var mockRepJob = new Mock<IRepositoryJob>();
            mockRepJob.Setup(x => x.GetJobsAsync()).Returns(MockGetJobs());

            

            //mock  IFormFile
            var content = "This is my cv";
            var fileName = "test.pdf";//set type to txt
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;
            IFormFile formFile = new FormFile(stream, 0, stream.Length, "id_from_form", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };
            var mockLogger = new Mock<ILogger<CareersController>>();

            var controller = new CareersController(mockRepAplication.Object, mockRepJob.Object, mockEmail.Object, mockLogger.Object);

            

            var viewModel = new CarrersViewModel
            {
                Aplication = new Aplication
                {
                    LastName = "Dragus",
                    FirstName = "Ciprian",
                    Email = "cipri.dragus@yaoo.com",
                    Phone = "0747343056",
                    Age = 28,
                    CoverLetter = "Salut",
                    CV = new CV { Id = 1}
                },
                FormFile = formFile,
            };

            //act 
            var result = await controller.PostAplication(viewModel) as RedirectToPageResult;


            //assert
            Assert.That(formFile.ContentType, Is.EqualTo("application/pdf"));
            Assert.That(result!.PageName, Is.EqualTo("/ClientInfo"));
            Assert.That(result!.RouteValues?["info"], Is.EqualTo("Aplicația ta a fost trimisă cu succes!"));
            mockRepAplication.Verify(x => x.SendAplication(It.IsAny<Aplication>()), Times.Once);
            mockEmail.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            
        }

        [Test]
        public async Task PostAplication_EmailServiceThrowsError_ReturnClientInfoRazorPage()
        {
            //arrange
            var mockRepAplication = new Mock<IRepositoryAplication>();
            var mockEmail = new Mock<IEmailService>();
            mockEmail.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Throws(new Exception("Eroare, Ceva nu a mers bine!"));
            var mockRepJob = new Mock<IRepositoryJob>();
            mockRepJob.Setup(x => x.GetJobsAsync()).Returns(MockGetJobs());



            //mock  IFormFile
            var content = "This is my cv";
            var fileName = "test.pdf";//set type to txt
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;
            IFormFile formFile = new FormFile(stream, 0, stream.Length, "id_from_form", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };
            var mockLogger = new Mock<ILogger<CareersController>>();

            var controller = new CareersController(mockRepAplication.Object, mockRepJob.Object, mockEmail.Object, mockLogger.Object);



            var viewModel = new CarrersViewModel
            {
                Aplication = new Aplication
                {
                    LastName = "Dragus",
                    FirstName = "Ciprian",
                    Email = "cipri.dragus@yaoo.com",
                    Phone = "0747343056",
                    Age = 28,
                    CoverLetter = "Salut",
                    CV = new CV { Id = 1 }
                },
                FormFile = formFile,
            };

            //act 
            var result = await controller.PostAplication(viewModel) as RedirectToPageResult;


            //assert
            Assert.That(formFile.ContentType, Is.EqualTo("application/pdf"));
            Assert.That(result!.PageName, Is.EqualTo("/ClientInfo"));
            Assert.That(result!.RouteValues?["info"], Is.EqualTo("Aplicația ta a fost trimisă cu succes!"));
            mockRepAplication.Verify(x => x.SendAplication(It.IsAny<Aplication>()), Times.Once);
            mockEmail.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            Assert.ThrowsAsync<Exception>(async () => await mockEmail.Object.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
        }
        [Test]
        public async Task PostAplication_AddApplicationToDatabaseThrowsError_ReturnClientInfoRazorPage()
        {
            //arrange
            var mockRepAplication = new Mock<IRepositoryAplication>();
            mockRepAplication.Setup(x => x.SendAplication(It.IsAny<Aplication>())).Throws(new Exception("Eroare, Ceva nu a mers bine!"));

            var mockEmail = new Mock<IEmailService>();
            var mockRepJob = new Mock<IRepositoryJob>();
            mockRepJob.Setup(x => x.GetJobsAsync()).Returns(MockGetJobs());



            //mock  IFormFile
            var content = "This is my cv";
            var fileName = "test.pdf";//set type to txt
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;
            IFormFile formFile = new FormFile(stream, 0, stream.Length, "id_from_form", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };
            var mockLogger = new Mock<ILogger<CareersController>>();

            var controller = new CareersController(mockRepAplication.Object, mockRepJob.Object, mockEmail.Object, mockLogger.Object);



            var viewModel = new CarrersViewModel
            {
                Aplication = new Aplication
                {
                    LastName = "Dragus",
                    FirstName = "Ciprian",
                    Email = "cipri.dragus@yaoo.com",
                    Phone = "0747343056",
                    Age = 28,
                    CoverLetter = "Salut",
                    CV = new CV { Id = 1 }
                },
                FormFile = formFile,
            };

            //act 
            var result = await controller.PostAplication(viewModel) as RedirectToPageResult;


            //assert
            Assert.That(formFile.ContentType, Is.EqualTo("application/pdf"));
            Assert.That(result!.PageName, Is.EqualTo("/ClientInfo"));
            Assert.That(result!.RouteValues?["info"], Is.EqualTo("Ceva nu a mers bine , încearcă dinou sau contactează dezvoltatorul!"));
            mockRepAplication.Verify(x => x.SendAplication(It.IsAny<Aplication>()), Times.Once);
            mockEmail.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            Assert.ThrowsAsync<Exception>(async () => await mockRepAplication.Object.SendAplication(It.IsAny<Aplication>()));
        }
    }
}
