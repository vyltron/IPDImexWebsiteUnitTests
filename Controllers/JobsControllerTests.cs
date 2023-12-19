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

namespace IPDImexWebsiteUnitTests.Controllers
{
    [TestFixture]
    internal class JobsControllerTests
    {
        private Mock<IRepositoryJob> _jobRepository;
        private Mock<ILogger<JobsController>> _logger;
        private JobsController _jobsController;

        [SetUp]
        public void SetUp()
        {
            _jobRepository = new Mock<IRepositoryJob>();
            _logger = new Mock<ILogger<JobsController>>();
            _jobsController = new JobsController(_jobRepository.Object, _logger.Object);
        }

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
            foreach (var job in jobs)
            {
                yield return job;
            }
            await Task.CompletedTask;

        }
        [Test]
        public async Task JobsPanel_CanLoadAndCountJobs_ReturnJobsView()
        {
            //arrange
            _jobRepository.Setup(x => x.GetJobsAsync()).Returns(MockGetJobs());
            _jobRepository.Setup(x => x.GetJobsCountAsync()).Returns(async () => await Task.FromResult(4));

            //act
            var result = (await _jobsController.JobsPanel() as ViewResult)?.ViewData.Model as JobsViewModel ?? new();
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
        #region DeleteJob

        [Test]
        public async Task DeleteJob_JobCannotBeDeleted_ReturnAdminInfo()
        {
            //arrange
            _jobRepository.Setup(x => x.DeleteJobAsync(It.IsAny<int>())).Returns(async () => await Task.FromResult(false));
            
            //act
            var result = await _jobsController.DeleteJob(2) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/AdminInfo"));
        }

        [Test]
        public async Task DeleteJob_JobIsDeleted_ReturnJobsPanelView()
        {
            //arrange
            _jobRepository.Setup(x => x.DeleteJobAsync(It.IsAny<int>())).Returns(async () => await Task.FromResult(true));
          
            //act
            var result = await _jobsController.DeleteJob(2) as RedirectToActionResult;

            //assert
            Assert.That(result?.ActionName, Is.EqualTo("JobsPanel"));
        }
        #endregion

        #region AddJob
        [Test]
        public async Task AddJob_ModelStateIsNotValid_ReturnJobsPanelView()
        {
            //arrange
            _jobRepository.Setup(x => x.GetJobsAsync()).Returns(MockGetJobs());
            _jobRepository.Setup(x => x.GetJobsCountAsync()).Returns(async () => await Task.FromResult(4));

            _jobsController.ModelState.AddModelError("error", "Test Error");

            //act
            var result = (await _jobsController.AddJob(new Job { JobName = "Test" }) as ViewResult)?.ViewData.Model as JobsViewModel ?? new();
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
        public async Task AddJob_EverythingIsFineButJobCouldNotBeAddedForSomeUnknownReason_ReturnAdminInfoPage()
        {
            //arrange
            _jobRepository.Setup(x => x.AddJob(It.IsAny<Job>())).Returns(async () => await Task.FromResult(false));
            Job job = new Job { JobName = "test" };

            //act
            var result = await _jobsController.AddJob(job) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/AdminInfo"));
            _jobRepository.Verify(x => x.AddJob(It.IsAny<Job>()), Times.Once());
        }

        [Test]
        public async Task AddJob_TheNewJobIsAdded_ReturnJobsPanelView()
        {
            //arrange
            _jobRepository.Setup(x => x.AddJob(It.IsAny<Job>())).Returns(async () => await Task.FromResult(true));
            Job job = new Job { JobName = "test" };

            //act
            var result = await _jobsController.AddJob(job) as RedirectToActionResult;

            //assert
            Assert.That(result?.ActionName, Is.EqualTo("JobsPanel"));
            _jobRepository.Verify(x => x.AddJob(It.IsAny<Job>()), Times.Once());
        }
        #endregion

        #region EditJob
        [Test]
        public async Task EditJob_JobDoesNotExistInTheDatabase_ReturnAdminInfoPage()
        {
            //arrange
            _jobRepository.Setup(x => x.JobExistById(It.IsAny<int>())).Returns(async () => await Task.FromResult(false));

            //act
            var result = await _jobsController.EditJob(new Job { JobName = "test" }, 1) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/AdminInfo"));
        }

        [Test]
        public async Task EditJob_ModelStateIsNotValid_Return()
        {
            //arrange
            _jobRepository.Setup(x => x.JobExistById(It.IsAny<int>())).Returns(async () => await Task.FromResult(true));
            _jobRepository.Setup(x => x.GetJobsAsync()).Returns(MockGetJobs());
            _jobRepository.Setup(x => x.GetJobsCountAsync()).Returns(async () => await Task.FromResult(4));

            _jobsController.ModelState.AddModelError("error", "error test");

            //act
            var result = (await _jobsController.EditJob(new Job { JobName = "test" }, 1) as ViewResult)?.ViewData.Model as JobsViewModel ?? new();
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
        public async Task EditJob_ForSomeReasonTHeJobCouldNotBeEdited_ReturnaAdminInfoPage()
        {
            //arrange
            _jobRepository.Setup(x => x.JobExistById(It.IsAny<int>())).Returns(async () => await Task.FromResult(true));
            _jobRepository.Setup(x => x.EditJob(It.IsAny<Job>())).Returns(async () => await Task.FromResult(false));
            Job job = new Job { JobName = "test" };

            //act
            var result = await _jobsController.EditJob(job, 1) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/AdminInfo"));
            _jobRepository.Verify(x => x.EditJob(It.IsAny<Job>()), Times.Once());
        }

        [Test]
        public async Task EditJob_ForSomeReasonTHeJobIsEdited_ReturnJobsPanelAction()
        {
            //arrange
            _jobRepository.Setup(x => x.JobExistById(It.IsAny<int>())).Returns(async () => await Task.FromResult(true));
            _jobRepository.Setup(x => x.EditJob(It.IsAny<Job>())).Returns(async () => await Task.FromResult(true));
            Job job = new Job { JobName = "test" };

            //act
            var result = await _jobsController.EditJob(job, 1) as RedirectToActionResult;

            //assert
            Assert.That(result?.ActionName, Is.EqualTo("JobsPanel"));
            _jobRepository.Verify(x => x.EditJob(It.IsAny<Job>()), Times.Once());
        }
        #endregion
    }
}
