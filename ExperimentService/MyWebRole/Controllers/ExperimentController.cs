using Microsoft.ServiceBus.Messaging;
using MyWebRole.Connectors;
using MyWebRole.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyWebRole.Controllers
{
    public class ExperimentController : Controller
    {
        TableConnector tableConnector = new TableConnector();
        QueueConnector busConnector = new QueueConnector(Properties.ExperimentsInitializationQueueName);

        private int experimentCount = 0;
        //
        // GET: /Experiment/

        public ActionResult Index()
        {
            List<ExperimentModels> experiments = tableConnector.RetrieveExperiments();
            experimentCount = experiments.Count;
            return View(experiments);
        }

        //
        // GET: /Experiment/Details/5

        public ActionResult Details(int id)
        {
            return View();
        }

        //
        // GET: /Experiment/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /Experiment/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ExperimentModels experiment)
        {
            if (ModelState.IsValid)
            {


                HttpPostedFileBase trainingFile = Request.Files["trainingFile"];


                BlobConnector trainingBlobConnector = new BlobConnector(Properties.TrainingIdFilesContainerName);
                experiment.trainingFileLink = trainingBlobConnector.UploadFile(trainingFile);
                experiment.trainingFile = trainingFile.FileName;


                HttpPostedFileBase testingFile = Request.Files["testingFile"];

                BlobConnector testingBlobConnector = new BlobConnector(Properties.TestingIdFilesContainerName);
                experiment.testingFileLink = testingBlobConnector.UploadFile(testingFile);
                experiment.testingFile = testingFile.FileName;

                experiment.id = (experimentCount + 1).ToString();
                experiment.startTime = DateTime.UtcNow.ToString();
                experiment.stopTime = DateTime.UtcNow.ToString();

                ExperimentModels insertModel = new ExperimentModels(experiment.id, experiment.experimentName, experiment.trainingFile, experiment.trainingFileLink, experiment.testingFile, experiment.testingFileLink, experiment.dimensionality, experiment.startTime, experiment.stopTime, string.Empty);
                tableConnector.InsertExperiment(insertModel);
                string model = JsonConvert.SerializeObject(insertModel);
                BrokeredMessage message = new BrokeredMessage();
                message.Properties["ExperimentModel"] = model;
                busConnector.SendMessage(message);

                //busConnector.SendMessage("Start new Experiment " + insertModel.experimentName, trainingFile.FileName, testingFile.FileName, insertModel.experimentName, insertModel.dimensionality);

                return RedirectToAction("Index");
            }

            return View();

        }

        //
        // GET: /Experiment/Edit/5

        public ActionResult Edit(int id)
        {
            return View();
        }

        //
        // POST: /Experiment/Edit/5

        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        //
        // GET: /Experiment/Delete/5

        public ActionResult Delete(int id)
        {
            return View();
        }

        //
        // POST: /Experiment/Delete/5

        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
