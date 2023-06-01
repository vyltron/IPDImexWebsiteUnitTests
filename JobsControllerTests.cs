using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using IPDImexWebsite.Controllers;
using IPDImexWebsite.Models;
using IPDImexWebsite.Models.Repository;
using IPDImexWebsite.ViewModels;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace IPDImexWebsiteUnitTests
{
    [TestFixture]
    internal class JobsControllerTests
    {
        public async IAsyncEnumerable<Job> MockGetJobs()
        {
            IEnumerable<Job> jobs = new List<Job>
            {
                new Job
                {
                    Id = 1,
                    JobName = "Test1",
                },
                new Job
                {
                    Id = 2,
                    JobName = "Test2",
                },
                new Job
                {
                    Id = 3,
                    JobName = "Test3",
                },
                new Job
                {
                    Id = 4,
                    JobName = "Test4",
                }
            };
            foreach(var job in jobs)
            {
                yield return job;
            }
            await Task.CompletedTask;

        }
        [Test]
        public async Task JobsPanel_CanLoadAndCountJobs_ReturnJobsView()
        {
            //arrange
            var mockJob = new Mock<IRepositoryJob>();
            var mockILogger = new Mock<ILogger<JobsController>>();
            mockJob.Setup(x => x.GetJobsAsync()).Returns(MockGetJobs());
            mockJob.Setup(x => x.GetJobsCountAsync()).Returns(async () => await Task.FromResult(4));

            var controller = new JobsController(mockJob.Object, mockILogger.Object);

            //act
            var result = (await controller.JobsPanel() as ViewResult)?.ViewData.Model as JobsViewModel ?? new();
            var jobList = result.Jobs.ToList();

            //assert 
            Assert.Multiple(() =>
            {
                Assert.That(jobList[0].Id, Is.EqualTo(4));
                Assert.That(jobList[0].JobName, Is.EqualTo("Test4"));
                Assert.That(jobList[3].Id, Is.EqualTo(1));
                Assert.That(jobList[3].JobName, Is.EqualTo("Test1"));

                Assert.That(result.TotalJobs, Is.EqualTo(4));
            });
        }
        [Test]
        public async Task JobsPanel_ThrowsException_ReturnAdminInfo()
        {
            //arrange
            var mockJob = new Mock<IRepositoryJob>();
            var mockILogger = new Mock<ILogger<JobsController>>();
            mockJob.Setup(x => x.GetJobsAsync()).Throws(new Exception("Eroare"));
            mockJob.Setup(x => x.GetJobsCountAsync()).Returns(async () => await Task.FromResult(4));

            var controller = new JobsController(mockJob.Object, mockILogger.Object);

            //act
            var result = await controller.JobsPanel() as RedirectToPageResult;

            //assert 
            Assert.That(result!.PageName, Is.EqualTo("/AdminInfo"));
            mockJob.Verify(x => x.GetJobsAsync(), Times.Once);
            Assert.ThrowsAsync<Exception>(async () =>
            {
                await foreach (var job in mockJob.Object.GetJobsAsync())
                {
                    //no code needed
                }
            }
            );
        }
        #region DeleteJob

        [Test]
        public async Task DeleteJob_JobCannotBeDeleted_ReturnAdminInfo()
        {
            //arrange
            var mockJob = new Mock<IRepositoryJob>();
            mockJob.Setup(x => x.DeleteJobAsync(It.IsAny<int>())).Returns(async () => await Task.FromResult(false));

            var mockILogger = new Mock<ILogger<JobsController>>();

            var controller = new JobsController(mockJob.Object, mockILogger.Object);

            //act
            var result = await controller.DeleteJob(2) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/AdminInfo"));
        }

        [Test]
        public async Task DeleteJob_JobIsDeleted_ReturnJobsPanelView()
        {
            //arrange
            var mockJob = new Mock<IRepositoryJob>();
            mockJob.Setup(x => x.DeleteJobAsync(It.IsAny<int>())).Returns(async () => await Task.FromResult(true));

            var mockILogger = new Mock<ILogger<JobsController>>();

            var controller = new JobsController(mockJob.Object, mockILogger.Object);

            //act
            var result = await controller.DeleteJob(2) as RedirectToActionResult;

            //assert
            Assert.That(result?.ActionName, Is.EqualTo("JobsPanel"));
        }
        [Test]
        public async Task DeleteJob_ThrowsException_ReturnAdminInfo()
        {
            //arrange
            var mockJob = new Mock<IRepositoryJob>();
            mockJob.Setup(x => x.DeleteJobAsync(It.IsAny<int>())).Throws(new Exception("error"));

            var mockILogger = new Mock<ILogger<JobsController>>();

            var controller = new JobsController(mockJob.Object, mockILogger.Object);

            //act
            var result = await controller.DeleteJob(2) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/AdminInfo"));
            mockJob.Verify(x => x.DeleteJobAsync(It.IsAny<int>()) ,Times.Once());
            Assert.ThrowsAsync<Exception>(async () => await mockJob.Object.DeleteJobAsync(2));
        }
        #endregion

        #region AddJob
        [Test]
        public async Task AddJob_ModelStateIsNotValid_ReturnJobsPanelView()
        {
            //arrange
            var mockJob = new Mock<IRepositoryJob>();
            mockJob.Setup(x => x.GetJobsAsync()).Returns(MockGetJobs());
            mockJob.Setup(x => x.GetJobsCountAsync()).Returns(async () => await Task.FromResult(4));
            var mockILogger = new Mock<ILogger<JobsController>>();

            var controller = new JobsController(mockJob.Object, mockILogger.Object);
            controller.ModelState.AddModelError("error", "Test Error");
            
            //act
            var result = (await controller.AddJob(new Job { JobName = "Test" }) as ViewResult)?.ViewData.Model as JobsViewModel ?? new();
            var jobList = result.Jobs.ToList();
          
            //assert
            Assert.Multiple(() =>
            {
                Assert.That(jobList[0].Id, Is.EqualTo(4));
                Assert.That(jobList[0].JobName, Is.EqualTo("Test4"));
                Assert.That(jobList[3].Id, Is.EqualTo(1));
                Assert.That(jobList[3].JobName, Is.EqualTo("Test1"));

                Assert.That(result.TotalJobs, Is.EqualTo(4));
                Assert.That(result.IsValidationError, Is.EqualTo(true));

            });
        }

        [Test]
        public async Task AddJob_ModelStateIsValidButSomehowJobIsNull_ReturnAdminInfoPage()
        {
            //arrange
            var mockJob = new Mock<IRepositoryJob>();
            var mockILogger = new Mock<ILogger<JobsController>>();

            var controller = new JobsController(mockJob.Object, mockILogger.Object);
          
            Job job = default(Job)!;

            //act
            var result = await controller.AddJob(job) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/AdminInfo"));
            mockJob.Verify(x => x.GetJobsAsync(), Times.Never());//pass the validation
        }
        
        [Test]
        [TestCase(null)]
        [TestCase("")]
        public async Task AddJob_ModelStateIsValidJobIsNotNullButSomehowJobNamePropertyIsNull_ReturnAdminInfoPage(string jobName)
        {
            //arrange
            var mockJob = new Mock<IRepositoryJob>();
            var mockILogger = new Mock<ILogger<JobsController>>();

            var controller = new JobsController(mockJob.Object, mockILogger.Object);

            Job job = new Job { JobName = jobName };

            //act
            var result = await controller.AddJob(job) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/AdminInfo"));
            mockJob.Verify(x => x.AddJob(It.IsAny<Job>()), Times.Never());
            mockJob.Verify(x => x.GetJobsAsync(), Times.Never());
        }

        [Test]
        public async Task AddJob_EverythingIsFineButJobCouldNotBeAddedForSomeUnknownReason_ReturnAdminInfoPage()
        {
            //arrange
            var mockJob = new Mock<IRepositoryJob>();
            var mockILogger = new Mock<ILogger<JobsController>>();
            mockJob.Setup(x => x.AddJob(It.IsAny<Job>())).Returns(async () => await Task.FromResult(false));


            var controller = new JobsController(mockJob.Object, mockILogger.Object);

            Job job = new Job { JobName = "test" };

            //act
            var result = await controller.AddJob(job) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/AdminInfo"));
            mockJob.Verify(x => x.AddJob(It.IsAny<Job>()), Times.Once());
        }

        [Test]
        public async Task AddJob_TheNewJobIsAdded_ReturnJobsPanelView()
        {
            //arrange
            var mockJob = new Mock<IRepositoryJob>();
            mockJob.Setup(x => x.AddJob(It.IsAny<Job>())).Returns(async () => await Task.FromResult(true));
            var mockILogger = new Mock<ILogger<JobsController>>();


            var controller = new JobsController(mockJob.Object, mockILogger.Object);

            Job job = new Job { JobName = "test" };

            //act
            var result = await controller.AddJob(job) as RedirectToActionResult;

            //assert
            Assert.That(result?.ActionName, Is.EqualTo("JobsPanel"));
            mockJob.Verify(x => x.AddJob(It.IsAny<Job>()), Times.Once());
        }

        [Test]
        public async Task AddJob_ThrowsException_ReturnAdminInfo()
        {
            //arrange
            var mockJob = new Mock<IRepositoryJob>();
            mockJob.Setup(x => x.AddJob(It.IsAny<Job>())).Throws(new Exception("eroare"));
            var mockILogger = new Mock<ILogger<JobsController>>();


            var controller = new JobsController(mockJob.Object, mockILogger.Object);

            Job job = new Job { JobName = "test" };

            //act
            var result = await controller.AddJob(job) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/AdminInfo"));
            mockJob.Verify(x => x.AddJob(It.IsAny<Job>()), Times.Once());
            Assert.ThrowsAsync<Exception>(async () => await mockJob.Object.AddJob(job));
        }
        #endregion

        #region EditJob
        [Test]
        public async Task EditJob_JobDoesNotExistInTheDatabase_ReturnAdminInfoPage()
        {
            //arrange
            var mockJob = new Mock<IRepositoryJob>();  
            mockJob.Setup(x => x.JobExistById(It.IsAny<int>())).Returns(async () => await Task.FromResult(false));

            var mockILogger = new Mock<ILogger<JobsController>>();


            var controller = new JobsController(mockJob.Object, mockILogger.Object);

            //act
            var result = await controller.EditJob(new Job { JobName ="test" }, 1) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/AdminInfo"));
        }

        [Test]
        public async Task EditJob_ModelStateIsNotValid_Return()
        {
            //arrange
            var mockJob = new Mock<IRepositoryJob>();
            mockJob.Setup(x => x.JobExistById(It.IsAny<int>())).Returns(async () => await Task.FromResult(true));
            mockJob.Setup(x => x.GetJobsAsync()).Returns(MockGetJobs());
            mockJob.Setup(x => x.GetJobsCountAsync()).Returns(async () => await Task.FromResult(4));
            var mockILogger = new Mock<ILogger<JobsController>>();

            var controller = new JobsController(mockJob.Object, mockILogger.Object);
            controller.ModelState.AddModelError("error", "error test");

            //act
            var result = (await controller.EditJob(new Job { JobName = "test" }, 1) as ViewResult)?.ViewData.Model as JobsViewModel ?? new();
            var jobList = result.Jobs.ToList();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(jobList[0].Id, Is.EqualTo(4));
                Assert.That(jobList[0].JobName, Is.EqualTo("Test4"));
                Assert.That(result.TotalJobs, Is.EqualTo(4));
                Assert.That(result.IsValidatioErrorEdit, Is.True);
            });
        }

        [Test]
        public async Task EditJob_JobValidationIsOkButJobObjectIsNull_ReturnaAdminInfoPage()
        {
            //arrange
            var mockJob = new Mock<IRepositoryJob>();
            mockJob.Setup(x => x.JobExistById(It.IsAny<int>())).Returns(async () => await Task.FromResult(true));
            var mockILogger = new Mock<ILogger<JobsController>>();


            var controller = new JobsController(mockJob.Object, mockILogger.Object);

            Job job = default(Job)!;

            //act
            var result = await controller.EditJob(job,1) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/AdminInfo"));
            mockJob.Verify(x => x.GetJobsAsync(), Times.Never());//pass the validation
            mockJob.Verify(x => x.EditJob(It.IsAny<Job>()), Times.Never());
        }
        
        [Test]
        [TestCase(null)]
        [TestCase("")]
        public async Task EditJob_JobValidationIsOkButJobNameInTheObjectIsNullOrEmpty_ReturnaAdminInfoPage(string jobName)
        {
            //arrange
            var mockJob = new Mock<IRepositoryJob>();
            mockJob.Setup(x => x.JobExistById(It.IsAny<int>())).Returns(async () => await Task.FromResult(true));
            var mockILogger = new Mock<ILogger<JobsController>>();


            var controller = new JobsController(mockJob.Object, mockILogger.Object);

            Job job = new Job { JobName = jobName};

            //act
            var result = await controller.EditJob(job, 1) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/AdminInfo"));
            mockJob.Verify(x => x.GetJobsAsync(), Times.Never());//pass the validation
            mockJob.Verify(x => x.EditJob(It.IsAny<Job>()), Times.Never());
        }
        [Test]
        public async Task EditJob_ForSomeReasonTHeJobCouldNotBeEdited_ReturnaAdminInfoPage()
        {
            //arrange
            var mockJob = new Mock<IRepositoryJob>();
            mockJob.Setup(x => x.JobExistById(It.IsAny<int>())).Returns(async () => await Task.FromResult(true));
            mockJob.Setup(x => x.EditJob(It.IsAny<Job>())).Returns(async () => await Task.FromResult(false));
            var mockILogger = new Mock<ILogger<JobsController>>();


            var controller = new JobsController(mockJob.Object, mockILogger.Object);

            Job job = new Job { JobName = "test" };

            //act
            var result = await controller.EditJob(job, 1) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/AdminInfo"));
            mockJob.Verify(x => x.EditJob(It.IsAny<Job>()), Times.Once());
        }

        [Test]
        public async Task EditJob_ForSomeReasonTHeJobIsEdited_ReturnJobsPanelAction()
        {
            //arrange
            var mockJob = new Mock<IRepositoryJob>();
            mockJob.Setup(x => x.JobExistById(It.IsAny<int>())).Returns(async () => await Task.FromResult(true));
            mockJob.Setup(x => x.EditJob(It.IsAny<Job>())).Returns(async () => await Task.FromResult(true));
            var mockILogger = new Mock<ILogger<JobsController>>();


            var controller = new JobsController(mockJob.Object, mockILogger.Object);

            Job job = new Job { JobName = "test" };

            //act
            var result = await controller.EditJob(job, 1) as RedirectToActionResult;

            //assert
            Assert.That(result?.ActionName, Is.EqualTo("JobsPanel"));
            mockJob.Verify(x => x.EditJob(It.IsAny<Job>()), Times.Once());
        }
        [Test]
        public async Task EditJob_ThrowsError_ReturnAdminInfo()
        {
            //arrange
            var mockJob = new Mock<IRepositoryJob>();
            mockJob.Setup(x => x.JobExistById(It.IsAny<int>())).Throws(new Exception("Error"));
            mockJob.Setup(x => x.EditJob(It.IsAny<Job>())).Returns(async () => await Task.FromResult(true));
            var mockILogger = new Mock<ILogger<JobsController>>();


            var controller = new JobsController(mockJob.Object, mockILogger.Object);

            Job job = new Job { JobName = "test" };

            //act
            var result = await controller.EditJob(job, 1) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/AdminInfo"));
            mockJob.Verify(x => x.JobExistById(It.IsAny<int>()), Times.Once());
            Assert.ThrowsAsync<Exception>(async () => await mockJob.Object.JobExistById(It.IsAny<int>()));
        }


        #endregion
    }
}
