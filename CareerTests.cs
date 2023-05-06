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

            var mockRepJob = new Mock<IRepositoryJob>();
            mockRepJob.Setup(x => x.GetJobs()).Returns(MockGetJobs());

            var controller = new CareersController(mockRepAplication.Object, mockRepJob.Object);

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
        public async Task PostAplication_ModelStateIsNotValid_ReturnIndexView()
        {
            //arrange
            var mockRepAplication = new Mock<IRepositoryAplication>();

            var mockRepJob = new Mock<IRepositoryJob>();
            mockRepJob.Setup(x => x.GetJobs()).Returns(MockGetJobs());

            var controller = new CareersController(mockRepAplication.Object, mockRepJob.Object);
            controller.ModelState.AddModelError("error", "Test error");

            var viewModel = new CarrersViewModel();

            //act 
            var result = await controller.PostAplication(viewModel) as ViewResult ?? new();


            //assert
            Assert.That(result.ViewData.ModelState.ContainsKey("error"), Is.EqualTo(true));
            Assert.That(result!.ViewName, Is.EqualTo("Index"));
        }

        [Test]
        public async Task PostAplication_FormFileIsNotNullButIsBiggerThan3mb_ReturnIndexView()
        {
            //arrange
            var mockRepAplication = new Mock<IRepositoryAplication>();

            var mockRepJob = new Mock<IRepositoryJob>();
            mockRepJob.Setup(x => x.GetJobs()).Returns(MockGetJobs());

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


            var controller = new CareersController(mockRepAplication.Object, mockRepJob.Object);


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
        public async Task PostAplication_FormFileIsNull_ReturnIndexView()
        {
            //arrange
            var mockRepAplication = new Mock<IRepositoryAplication>();

            var mockRepJob = new Mock<IRepositoryJob>();
            mockRepJob.Setup(x => x.GetJobs()).Returns(MockGetJobs());

            //mock  IFormFile

            var controller = new CareersController(mockRepAplication.Object, mockRepJob.Object);


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
        public async Task PostAplication_FormFileIsNotPDF_ReturnIndexView()
        {
            //arrange
            var mockRepAplication = new Mock<IRepositoryAplication>();

            var mockRepJob = new Mock<IRepositoryJob>();
            mockRepJob.Setup(x => x.GetJobs()).Returns(MockGetJobs());

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


            var controller = new CareersController(mockRepAplication.Object, mockRepJob.Object);


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
        public async Task PostAplication_FormFileIsPDFAndCVFileIsNotNull_ReturnClientInfoRazorPage()
        {
            //arrange
            var mockRepAplication = new Mock<IRepositoryAplication>();

            var mockRepJob = new Mock<IRepositoryJob>();
            mockRepJob.Setup(x => x.GetJobs()).Returns(MockGetJobs());

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


            var controller = new CareersController(mockRepAplication.Object, mockRepJob.Object);

            

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
            
        }
    }
}
