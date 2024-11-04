using AutoMapper;
using Indotalent.Applications.InventoryTransactions;
using Indotalent.Applications.NumberSequences;
using Indotalent.Applications.Products;
using Indotalent.Applications.TransferIns;
using Indotalent.Applications.TransferOuts;
using Indotalent.Data;
using Indotalent.Infrastructures.Repositories;
using Indotalent.Models.Entities;
using Indotalent.Models.Enums;
using Indotalent.Pages.TransferIns;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace TestProject1
{
    [TestFixture]
    public class TransferInTests
    {
        private Mock<IMapper> _mapperMock;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private ApplicationDbContext _dbContext;
        private NumberSequenceService _numberSequenceService;
        private TransferInFormModel _transferInFormModel;

        [SetUp]
        public void Setup()
        {
            _mapperMock = new Mock<IMapper>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _auditColumnTransformerMock = new Mock<IAuditColumnTransformer>();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _dbContext = new ApplicationDbContext(options);
            _numberSequenceService = new NumberSequenceService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            var transferOutService = new TransferOutService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            var productService = new ProductService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            var transferInService = new TransferInService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            var inventoryTransactionService = new InventoryTransactionService(null, _dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            _transferInFormModel = new TransferInFormModel(
                _mapperMock.Object,
                transferInService,
                _numberSequenceService,
                transferOutService,
                productService,
                inventoryTransactionService
            );

            _transferInFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            _transferInFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }

        [Test]
        public async Task OnPostAsync_AgregarNuevaTransferIn()
        {
            // Arrange
            var newTransferInModel = new TransferInFormModel.TransferInModel
            {
                TransferOutId = 1,
                TransferReceiveDate = DateTime.Now,
                Status = TransferStatus.Confirmed,
                Description = "Descripción test"
            };

            _transferInFormModel.TransferInForm = newTransferInModel;
            _transferInFormModel.Action = "create";

            var mappedTransferIn = new TransferIn
            {
                RowGuid = Guid.NewGuid(),
                TransferOutId = newTransferInModel.TransferOutId,
                TransferReceiveDate = newTransferInModel.TransferReceiveDate,
                Status = newTransferInModel.Status,
                Description = newTransferInModel.Description
            };

            // Configura el mock
            _mapperMock.Setup(mapper => mapper.Map<TransferIn>(newTransferInModel)).Returns(mappedTransferIn);

            _transferInFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _transferInFormModel.OnPostAsync(newTransferInModel);

            // Verifica si el resultado es de tipo RedirectResult y obtener la URL
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define la URL esperada de redirección
            string expectedUrl = $"./TransferInForm?rowGuid={mappedTransferIn.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;

            // Assert
            Assert.AreEqual("Success create new data.", _transferInFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            // Verifica el número generado cumple con el formato esperado
            var expectedPrefix = "IN";
            var expectedDate = DateTime.Now.ToString("yyyyMMdd");

            Console.WriteLine("TransferIn Number: " + mappedTransferIn.Number);

            Assert.IsTrue(mappedTransferIn.Number.EndsWith(expectedPrefix), $"El prefijo esperado es '{expectedPrefix}' pero se obtuvo '{mappedTransferIn.Number.Substring(mappedTransferIn.Number.Length - 2)}'");
            Assert.IsTrue(mappedTransferIn.Number.Contains(expectedDate), $"La fecha esperada es '{expectedDate}' pero no se encontró en '{mappedTransferIn.Number}'");

            // Verifica que el nuevo registro exista en la BD
            var createdTransferIn = await _dbContext.TransferIn
                .FirstOrDefaultAsync(ti => ti.RowGuid == mappedTransferIn.RowGuid);
            Assert.IsNotNull(createdTransferIn, "El registro debe existir en la BD.");
        }



        [Test]
        public async Task OnPostAsync_EliminarTransferIn()
        {
            // Arrange
            var newTransferInModel = new TransferInFormModel.TransferInModel
            {
                TransferOutId = 1,
                TransferReceiveDate = DateTime.Now,
                Status = TransferStatus.Confirmed,
                Description = "Descripción test"
            };

            _transferInFormModel.TransferInForm = newTransferInModel;

            // Crea y agrega el objeto TransferIn a la BD
            var mappedTransferIn = new TransferIn
            {
                RowGuid = Guid.NewGuid(),
                TransferOutId = newTransferInModel.TransferOutId,
                TransferReceiveDate = newTransferInModel.TransferReceiveDate,
                Status = newTransferInModel.Status,
                Description = newTransferInModel.Description
            };

            await _dbContext.TransferIn.AddAsync(mappedTransferIn);
            await _dbContext.SaveChangesAsync();

            // Guarda el RowGuid del TransferIn recién creado para usarlo en la eliminación
            var createdRowGuid = mappedTransferIn.RowGuid;

            // Configura el formulario de eliminación usando el RowGuid del TransferIn creado
            _transferInFormModel.TransferInForm = new TransferInFormModel.TransferInModel
            {
                RowGuid = createdRowGuid
            };

            // Cambia el Request.Query["action"] a "delete" para simular la acción de eliminación
            _transferInFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "delete" }
            });

            // Act 
            var deleteResult = await _transferInFormModel.OnPostAsync(_transferInFormModel.TransferInForm);

            // Verifica si el resultado es de tipo RedirectResult y obtiene la URL
            var redirectResult = deleteResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define la URL esperada de redirección después de eliminar
            string expectedUrl = $"./TransferInList";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success delete existing data.", _transferInFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success delete existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            // Verifica que el registro haya sido eliminado de la BD
            var deletedTransferIn = await _dbContext.TransferIn.SingleOrDefaultAsync(ti => ti.RowGuid == createdRowGuid);
            Assert.IsNotNull(deletedTransferIn, "El registro debe existir en la base de datos.");
            Assert.IsFalse(deletedTransferIn.IsNotDeleted, "El registro debe estar marcado como eliminado (IsNotDeleted = false).");
        }


    }
}
