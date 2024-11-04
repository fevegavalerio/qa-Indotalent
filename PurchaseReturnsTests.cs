using AutoMapper;
using Indotalent.Applications.PurchaseReturns;
using Indotalent.Applications.GoodsReceives;
using Indotalent.Applications.InventoryTransactions;
using Indotalent.Applications.NumberSequences;
using Indotalent.Applications.Products;
using Indotalent.Applications.Warehouses;
using Indotalent.Data;
using Indotalent.Models.Entities;
using Indotalent.Models.Enums;
using Indotalent.Pages.PurchaseReturns;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Indotalent.Infrastructures.Repositories;

namespace TestProject1
{
    [TestFixture]
    public class PurchaseReturnsTests
    {
        private Mock<IMapper> _mapperMock;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private ApplicationDbContext _dbContext;
        private NumberSequenceService _numberSequenceService;
        private PurchaseReturnFormModel _purchaseReturnFormModel;

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

            var goodsReceiveService = new GoodsReceiveService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            var productService = new ProductService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            var warehouseService = new WarehouseService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            var purchaseReturnService = new PurchaseReturnService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            var inventoryTransactionService = new InventoryTransactionService(warehouseService, _dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            _purchaseReturnFormModel = new PurchaseReturnFormModel(
                _mapperMock.Object,
                purchaseReturnService,
                _numberSequenceService,
                goodsReceiveService,
                productService,
                warehouseService,
                inventoryTransactionService
            );

            _purchaseReturnFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            _purchaseReturnFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }

        [Test]
        public async Task OnPostAsync_AgregarNuevoPurchaseReturn()
        {
            // Arrange
            var newPurchaseReturnModel = new PurchaseReturnFormModel.PurchaseReturnModel
            {
                GoodsReceiveId = 1,
                ReturnDate = DateTime.Now,
                Status = PurchaseReturnStatus.Confirmed,
                Description = "Descripción test"
            };

            _purchaseReturnFormModel.PurchaseReturnForm = newPurchaseReturnModel;
            _purchaseReturnFormModel.Action = "create";

            var mappedPurchaseReturn = new PurchaseReturn
            {
                RowGuid = Guid.NewGuid(),
                GoodsReceiveId = newPurchaseReturnModel.GoodsReceiveId,
                ReturnDate = newPurchaseReturnModel.ReturnDate,
                Status = newPurchaseReturnModel.Status,
                Description = newPurchaseReturnModel.Description
            };

            // Configura el mock
            _mapperMock.Setup(mapper => mapper.Map<PurchaseReturn>(newPurchaseReturnModel)).Returns(mappedPurchaseReturn);

            _purchaseReturnFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _purchaseReturnFormModel.OnPostAsync(newPurchaseReturnModel);

            // Verifica si el resultado es de tipo RedirectResult y obtiene la URL
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define la URL esperada de redirección
            string expectedUrl = $"./PurchaseReturnForm?rowGuid={mappedPurchaseReturn.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;

            // Assert de redirección y mensaje de estado
            Assert.AreEqual("Success create new data.", _purchaseReturnFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            // Verifica que el número generado cumple con el formato esperado
            var expectedPrefix = "PRN";
            var expectedDate = DateTime.Now.ToString("yyyyMMdd");

            Console.WriteLine("PurchaseReturn Number: " + mappedPurchaseReturn.Number);

            Assert.IsTrue(mappedPurchaseReturn.Number.EndsWith(expectedPrefix), $"El prefijo esperado es '{expectedPrefix}' pero se obtuvo '{mappedPurchaseReturn.Number.Substring(mappedPurchaseReturn.Number.Length - 3)}'");
            Assert.IsTrue(mappedPurchaseReturn.Number.Contains(expectedDate), $"La fecha esperada es '{expectedDate}' pero no se encontró en '{mappedPurchaseReturn.Number}'");

            // Verifica que el nuevo registro exista en la base de datos
            var createdPurchaseReturn = await _dbContext.PurchaseReturn
                .FirstOrDefaultAsync(pr => pr.RowGuid == mappedPurchaseReturn.RowGuid);
            Assert.IsNotNull(createdPurchaseReturn, "El registro debe existir en la BD.");
        }


        [Test]
        public async Task OnPostAsync_EditarPurchaseReturn()
        {
            // Arrange
            var newPurchaseReturnModel = new PurchaseReturnFormModel.PurchaseReturnModel
            {
                GoodsReceiveId = 1,
                ReturnDate = DateTime.Now,
                Status = PurchaseReturnStatus.Confirmed,
                Description = "Descripción test"
            };

            _purchaseReturnFormModel.PurchaseReturnForm = newPurchaseReturnModel;

            _purchaseReturnFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "create" }
            });

            var mappedPurchaseReturn = new PurchaseReturn
            {
                RowGuid = Guid.NewGuid(),
                GoodsReceiveId = newPurchaseReturnModel.GoodsReceiveId,
                ReturnDate = newPurchaseReturnModel.ReturnDate,
                Status = newPurchaseReturnModel.Status,
                Description = newPurchaseReturnModel.Description
            };

            _mapperMock.Setup(mapper => mapper.Map<PurchaseReturn>(newPurchaseReturnModel)).Returns(mappedPurchaseReturn);
            _purchaseReturnFormModel.TempData["StatusMessage"] = string.Empty;

            // Act 1
            var createResult = await _purchaseReturnFormModel.OnPostAsync(newPurchaseReturnModel);
            var createdRowGuid = mappedPurchaseReturn.RowGuid;

            // Arrange
            var editedPurchaseReturnModel = new PurchaseReturnFormModel.PurchaseReturnModel
            {
                RowGuid = createdRowGuid,
                GoodsReceiveId = 1,
                ReturnDate = DateTime.Now.AddDays(1), 
                Status = PurchaseReturnStatus.Archived,  
                Description = "Descripción actualizada"
            };

            _purchaseReturnFormModel.PurchaseReturnForm = editedPurchaseReturnModel;

            // Cambia el Request.Query["action"] a "edit" para simular la acción de edición
            _purchaseReturnFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "edit" }
            });

            // Configura el mock 
            _mapperMock.Setup(mapper => mapper.Map(editedPurchaseReturnModel, mappedPurchaseReturn))
                       .Callback((PurchaseReturnFormModel.PurchaseReturnModel source, PurchaseReturn destination) =>
                       {
                           destination.ReturnDate = source.ReturnDate;
                           destination.Status = source.Status;
                           destination.Description = source.Description;
                       });

            // Act 2 
            var editResult = await _purchaseReturnFormModel.OnPostAsync(editedPurchaseReturnModel);

            // Verifica si el resultado es de tipo RedirectResult y obtiene la URL
            var redirectResult = editResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define la URL esperada de redirección
            string expectedUrl = $"./PurchaseReturnForm?rowGuid={editedPurchaseReturnModel.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success update existing data.", _purchaseReturnFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success update existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            // Verifica que los valores actualizados del registro existan en la base de datos
            var updatedPurchaseReturn = await _dbContext.PurchaseReturn
                .FirstOrDefaultAsync(pr => pr.RowGuid == editedPurchaseReturnModel.RowGuid);

            Assert.IsNotNull(updatedPurchaseReturn, "El registro actualizado debe existir en la BD.");
            Assert.AreEqual(editedPurchaseReturnModel.ReturnDate, updatedPurchaseReturn.ReturnDate, "Campo ReturnDate actualizado correctamente.");
            Assert.AreEqual(editedPurchaseReturnModel.Status, updatedPurchaseReturn.Status, "Campo Status actualizado correctamente.");
            Assert.AreEqual(editedPurchaseReturnModel.Description, updatedPurchaseReturn.Description, "Campo Description actualizado correctamente.");
        }



        [Test]
        public async Task OnPostAsync_EliminarPurchaseReturn()
        {
            // Arrange
            var newPurchaseReturnModel = new PurchaseReturnFormModel.PurchaseReturnModel
            {
                GoodsReceiveId = 1,
                ReturnDate = DateTime.Now,
                Status = PurchaseReturnStatus.Confirmed,
                Description = "Descripción test"
            };

            _purchaseReturnFormModel.PurchaseReturnForm = newPurchaseReturnModel;

            // Crea y agregar el objeto PurchaseReturn a la BD
            var mappedPurchaseReturn = new PurchaseReturn
            {
                RowGuid = Guid.NewGuid(),
                GoodsReceiveId = newPurchaseReturnModel.GoodsReceiveId,
                ReturnDate = newPurchaseReturnModel.ReturnDate,
                Status = newPurchaseReturnModel.Status,
                Description = newPurchaseReturnModel.Description
            };

            await _dbContext.PurchaseReturn.AddAsync(mappedPurchaseReturn);
            await _dbContext.SaveChangesAsync();

            // Guarda el RowGuid del PurchaseReturn recién creado para usarlo en la eliminación
            var createdRowGuid = mappedPurchaseReturn.RowGuid;

            // Configura el formulario de eliminación usando el RowGuid del PurchaseReturn creado
            _purchaseReturnFormModel.PurchaseReturnForm = new PurchaseReturnFormModel.PurchaseReturnModel
            {
                RowGuid = createdRowGuid
            };

            // Cambia el Request.Query["action"] a "delete" para simular la acción de eliminación
            _purchaseReturnFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "delete" }
            });

            // Act - Ejecutar la acción de eliminación
            var deleteResult = await _purchaseReturnFormModel.OnPostAsync(_purchaseReturnFormModel.PurchaseReturnForm);

            // Verifica si el resultado es de tipo RedirectResult y obtiene la URL
            var redirectResult = deleteResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define la URL esperada de redirección después de eliminar
            string expectedUrl = $"./PurchaseReturnList";
            string actualUrl = redirectResult.Url;

            // Assert
            Assert.AreEqual("Success delete existing data.", _purchaseReturnFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success delete existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            // Verifica que el registro haya sido eliminado de la BD
            var deletedPurchaseReturn = await _dbContext.PurchaseReturn.SingleOrDefaultAsync(gr => gr.RowGuid == createdRowGuid);
            Assert.IsNotNull(deletedPurchaseReturn, "El registro debe existir en la base de datos.");
            Assert.IsFalse(deletedPurchaseReturn.IsNotDeleted, "El registro debe estar marcado como eliminado (IsNotDeleted = false).");
        }



        [Test]
        public async Task OnPostAsync_ModeloInvalido_MostrarMensajeFaltaGoodsReceiveId()
        {
            // Arrange
            var invalidPurchaseReturnModel = new PurchaseReturnFormModel.PurchaseReturnModel
            {
                GoodsReceiveId = 0, 
                ReturnDate = DateTime.Now,
                Status = PurchaseReturnStatus.Confirmed,
                Description = "Descripción test"
            };

            _purchaseReturnFormModel.PurchaseReturnForm = invalidPurchaseReturnModel;
            _purchaseReturnFormModel.ModelState.AddModelError("GoodsReceiveId", "The GoodsReceiveId field is required.");

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(async () => await _purchaseReturnFormModel.OnPostAsync(invalidPurchaseReturnModel));
            Assert.IsNotNull(ex);
            Assert.IsTrue(ex.Message.Contains("The GoodsReceiveId field is required."), $"Expected error message: 'The GoodsReceiveId field is required.' but got: '{ex.Message}'");
        }



    }
}
