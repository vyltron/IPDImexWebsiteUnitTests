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
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;

namespace IPDImexWebsiteUnitTests.Controllers
{
    [TestFixture]
    internal class CareerTests
    {

        private Mock<IRepositoryAplication> _appRepository;
        private Mock<ILogger<CareersController>> _logger;
        private Mock<IRepositoryJob> _jobRepository;
        private Mock<IEmailService> _emailService;
        private Mock<IFileWrapper> _fileWrapper;
        private CareersController _controller;

        [SetUp]
        public void SetUp()
        {
            _appRepository = new Mock<IRepositoryAplication>();
            _logger = new Mock<ILogger<CareersController>>();
            _jobRepository = new Mock<IRepositoryJob>();
            _emailService = new Mock<IEmailService>();
            _fileWrapper = new Mock<IFileWrapper>();
            _controller = new CareersController(_appRepository.Object, _jobRepository.Object,
                                                _emailService.Object, _logger.Object, _fileWrapper.Object, new Mock<IWebHostEnvironment>().Object);
        }

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

            _jobRepository.Setup(x => x.GetJobsAsync()).Returns(MockGetJobs());

            //act 
            var results = (await _controller.Index() as ViewResult)?.ViewData.Model as CarrersViewModel ?? new();
            var jobs = results.Jobs.ToList();
            //assert
            Assert.Multiple(() =>
            {
                Assert.That(jobs, Has.Count.EqualTo(3));
                Assert.That(jobs[0].Id, Is.EqualTo(1));
                Assert.That(jobs[0].JobName, Is.EqualTo("Job1"));
                Assert.That(jobs[2].Id, Is.EqualTo(3));
                Assert.That(jobs[2].JobName, Is.EqualTo("Job3"));
            });
        }

        [Test]
        public async Task PostAplication_ModelStateIsNotValid_ReturnIndexView()
        {
            //arrange

            _jobRepository.Setup(x => x.GetJobsAsync()).Returns(MockGetJobs());


            _controller.ModelState.AddModelError("error", "Test error");

            var viewModel = new CarrersViewModel();

            //act 
            var result = await _controller.PostAplication(viewModel) as ViewResult ?? new();


            //assert
            Assert.That(result.ViewData.ModelState.ContainsKey("error"), Is.EqualTo(true));
            Assert.That(result!.ViewName, Is.EqualTo("Index"));
        }

        [Test]
        public async Task PostAplication_FormFileIsNull_ReturnIndexView()
        {
            //arrange
            _jobRepository.Setup(x => x.GetJobsAsync()).Returns(MockGetJobs());

            var mockLogger = new Mock<ILogger<CareersController>>();

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
            var result = await _controller.PostAplication(viewModel) as ViewResult ?? new();

            //assert
            Assert.That(result!.ViewName, Is.EqualTo("Index"));
            Assert.That(result.ViewData.ModelState.ContainsKey("MissingCV"), Is.EqualTo(true));
        }

        [Test]
        public async Task PostAplication_FormFileIsNotNullButIsBiggerThan3mb_ReturnIndexView()
        {
            //arrange
            _jobRepository.Setup(x => x.GetJobsAsync()).Returns(MockGetJobs());

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
            var result = await _controller.PostAplication(viewModel) as ViewResult ?? new();

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
            _jobRepository.Setup(x => x.GetJobsAsync()).Returns(MockGetJobs());

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
            var result = await _controller.PostAplication(viewModel) as ViewResult ?? new();

            //assert
            Assert.That(formFile.ContentType, Is.EqualTo("text/plain"));//check the opposite as in the method
            Assert.That(result!.ViewName, Is.EqualTo("Index"));
            Assert.That(result.ViewData.ModelState.ContainsKey("NotPDFType"), Is.EqualTo(true));
        }

        [Test]
        public async Task PostAplication_EverythingWorks_ReturnClientInfoRazorPage()
        {
            //arrange
            _emailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(async () => await Task.FromResult(Task.CompletedTask));
            _jobRepository.Setup(x => x.GetJobsAsync()).Returns(MockGetJobs());

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
            var result = await _controller.PostAplication(viewModel) as RedirectToPageResult;


            //assert
            Assert.That(formFile.ContentType, Is.EqualTo("application/pdf"));
            Assert.That(result!.PageName, Is.EqualTo("/ClientInfo"));
            Assert.That(result!.RouteValues?["info"], Is.EqualTo("Aplicația ta a fost trimisă cu succes!"));
            _appRepository.Verify(x => x.SendAplication(It.IsAny<Aplication>()), Times.Once);
            _emailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.AtLeast(3));

        }

        [Test]
        public async Task PostAplication_EmailServiceThrowsError_ReturnClientInfoRazorPage()
        {
            //arrange
            _emailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Throws(new Exception("Eroare, Ceva nu a mers bine!"));
            _jobRepository.Setup(x => x.GetJobsAsync()).Returns(MockGetJobs());

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
            var result = await _controller.PostAplication(viewModel) as RedirectToPageResult;

            //assert
            Assert.That(formFile.ContentType, Is.EqualTo("application/pdf"));
            Assert.That(result!.PageName, Is.EqualTo("/ClientInfo"));
            Assert.That(result!.RouteValues?["info"], Is.EqualTo("Aplicația ta a fost trimisă cu succes!"));
            _appRepository.Verify(x => x.SendAplication(It.IsAny<Aplication>()), Times.Once);
            _emailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            Assert.ThrowsAsync<Exception>(async () => await _emailService.Object.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
        }
    }
}
