using AutoMapper;
using Indotalent.Applications.GoodsReceives;
using Indotalent.Applications.InventoryTransactions;
using Indotalent.Applications.NumberSequences;
using Indotalent.Applications.Products;
using Indotalent.Applications.PurchaseOrders;
using Indotalent.Applications.Warehouses;
using Indotalent.Data;
using Indotalent.Infrastructures.Repositories;
using Indotalent.Models.Entities;
using Indotalent.Models.Enums;
using Indotalent.Pages.GoodsReceives;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestProject1
{
    [TestFixture]
    public class GoodsReceivesTests
    {
        private Mock<IMapper> _mapperMock;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private ApplicationDbContext _dbContext;
        private NumberSequenceService _numberSequenceService;
        private GoodsReceiveFormModel _goodsReceiveFormModel;

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

            var productService = new ProductService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            var purchaseOrderService = new PurchaseOrderService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            var warehouseService = new WarehouseService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            var goodsReceiveService = new GoodsReceiveService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            var inventoryTransactionService = new InventoryTransactionService(warehouseService, _dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            _goodsReceiveFormModel = new GoodsReceiveFormModel(
                _mapperMock.Object,
                goodsReceiveService,
                _numberSequenceService,
                purchaseOrderService,
                productService,
                warehouseService,
                inventoryTransactionService
            );

            _goodsReceiveFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            _goodsReceiveFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }

        [Test]
        public async Task OnPostAsync_AgregarNuevoGoodReceive()
        {
            // Arrange
            var newGoodsReceiveModel = new GoodsReceiveFormModel.GoodsReceiveModel
            {
                PurchaseOrderId = 1,
                ReceiveDate = DateTime.Now,
                Status = GoodsReceiveStatus.Confirmed,
                Description = "Descripción test"
            };

            _goodsReceiveFormModel.GoodsReceiveForm = newGoodsReceiveModel;
            _goodsReceiveFormModel.Action = "create";

            var mappedGoodsReceive = new GoodsReceive
            {
                RowGuid = Guid.NewGuid(),
                PurchaseOrderId = newGoodsReceiveModel.PurchaseOrderId,
                ReceiveDate = newGoodsReceiveModel.ReceiveDate,
                Status = newGoodsReceiveModel.Status,
                Description = newGoodsReceiveModel.Description
            };

            // Configura
            _mapperMock.Setup(mapper => mapper.Map<GoodsReceive>(newGoodsReceiveModel)).Returns(mappedGoodsReceive);

            _goodsReceiveFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _goodsReceiveFormModel.OnPostAsync(newGoodsReceiveModel);

            // Verifica si el resultado es de tipo RedirectResult y obtiene la URL
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define la URL esperada de redirección
            string expectedUrl = $"./GoodsReceiveForm?rowGuid={mappedGoodsReceive.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;

            // Assert
            Assert.AreEqual("Success create new data.", _goodsReceiveFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            // Verifica el número generado cumple con el formato esperado
            var expectedPrefix = "GR";
            var expectedDate = DateTime.Now.ToString("yyyyMMdd");

            Console.WriteLine("GoodsReceive Number: " + mappedGoodsReceive.Number);

           
            Assert.IsTrue(mappedGoodsReceive.Number.EndsWith(expectedPrefix), $"El prefijo esperado es '{expectedPrefix}' pero se obtuvo '{mappedGoodsReceive.Number.Substring(mappedGoodsReceive.Number.Length - 2)}'");
            Assert.IsTrue(mappedGoodsReceive.Number.Contains(expectedDate), $"La fecha esperada es '{expectedDate}' pero no se encontró en '{mappedGoodsReceive.Number}'");

            // Verifica que el nuevo registro exista en la base de datos
            var createdGoodsReceive = await _dbContext.GoodsReceive
                .FirstOrDefaultAsync(gr => gr.RowGuid == mappedGoodsReceive.RowGuid);
            Assert.IsNotNull(createdGoodsReceive, "El registro debe existir en la BD.");
        }


        [Test]
        public async Task OnPostAsync_EditarGoodReceive()
        {
            // Arrange 
            var newGoodsReceiveModel = new GoodsReceiveFormModel.GoodsReceiveModel
            {
                PurchaseOrderId = 1,
                ReceiveDate = DateTime.Now,
                Status = GoodsReceiveStatus.Confirmed,
                Description = "Descripción original"
            };

            _goodsReceiveFormModel.GoodsReceiveForm = newGoodsReceiveModel;

            _goodsReceiveFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "create" }
            });

            var mappedGoodsReceive = new GoodsReceive
            {
                RowGuid = Guid.NewGuid(),
                PurchaseOrderId = newGoodsReceiveModel.PurchaseOrderId,
                ReceiveDate = newGoodsReceiveModel.ReceiveDate,
                Status = newGoodsReceiveModel.Status,
                Description = newGoodsReceiveModel.Description
            };

            _mapperMock.Setup(mapper => mapper.Map<GoodsReceive>(newGoodsReceiveModel)).Returns(mappedGoodsReceive);
            _goodsReceiveFormModel.TempData["StatusMessage"] = string.Empty;

            // Act 1 
            var createResult = await _goodsReceiveFormModel.OnPostAsync(newGoodsReceiveModel);
            var createdRowGuid = mappedGoodsReceive.RowGuid;

            // Arrange 
            var editedGoodsReceiveModel = new GoodsReceiveFormModel.GoodsReceiveModel
            {
                RowGuid = createdRowGuid,
                PurchaseOrderId = 1,
                ReceiveDate = DateTime.Now.AddDays(1), 
                Status = GoodsReceiveStatus.Archived,  
                Description = "Descripción actualizada"
            };

            _goodsReceiveFormModel.GoodsReceiveForm = editedGoodsReceiveModel;

            // Cambia el Request.Query["action"] a "edit" para simular la acción de edición
            _goodsReceiveFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "edit" }
            });

            // Configura el mock
            _mapperMock.Setup(mapper => mapper.Map(editedGoodsReceiveModel, mappedGoodsReceive))
                       .Callback((GoodsReceiveFormModel.GoodsReceiveModel source, GoodsReceive destination) =>
                {
                    destination.ReceiveDate = source.ReceiveDate;
                    destination.Status = source.Status;
                    destination.Description = source.Description;
                });

            // Act 2
            var editResult = await _goodsReceiveFormModel.OnPostAsync(editedGoodsReceiveModel);

            // Verifica si el resultado es de tipo RedirectResult y obtener la URL
            var redirectResult = editResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define la URL esperada de redirección
            string expectedUrl = $"./GoodsReceiveForm?rowGuid={editedGoodsReceiveModel.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success update existing data.", _goodsReceiveFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success update existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            // Verifica que los valores del registro hayan sido actualizados en la BD
            var updatedGoodsReceive = await _dbContext.GoodsReceive
                .FirstOrDefaultAsync(gr => gr.RowGuid == editedGoodsReceiveModel.RowGuid);

            Assert.IsNotNull(updatedGoodsReceive, "El registro actualizado debe existir en la BD.");
            Assert.AreEqual(editedGoodsReceiveModel.ReceiveDate, updatedGoodsReceive.ReceiveDate, "Campo ReceiveDate actualizado correctamente.");
            Assert.AreEqual(editedGoodsReceiveModel.Status, updatedGoodsReceive.Status, "Campo Status actualizado correctamente.");
            Assert.AreEqual(editedGoodsReceiveModel.Description, updatedGoodsReceive.Description, "Campo Description actualizado correctamente.");
        }

        [Test]
        public async Task OnPostAsync_EliminarGoodReceive()
        {
            // Arrange
            var newGoodsReceiveModel = new GoodsReceiveFormModel.GoodsReceiveModel
            {
                PurchaseOrderId = 1,
                ReceiveDate = DateTime.Now,
                Status = GoodsReceiveStatus.Confirmed,
                Description = "Descripción del GoodsReceive para eliminar"
            };

            _goodsReceiveFormModel.GoodsReceiveForm = newGoodsReceiveModel;

            // Crea y agrega el objeto GoodsReceive a la BD
            var mappedGoodsReceive = new GoodsReceive
            {
                RowGuid = Guid.NewGuid(),
                PurchaseOrderId = newGoodsReceiveModel.PurchaseOrderId,
                ReceiveDate = newGoodsReceiveModel.ReceiveDate,
                Status = newGoodsReceiveModel.Status,
                Description = newGoodsReceiveModel.Description
            };

            await _dbContext.GoodsReceive.AddAsync(mappedGoodsReceive);
            await _dbContext.SaveChangesAsync();

            // Guarda el RowGuid del GoodsReceive recién creado para usarlo en la eliminación
            var createdRowGuid = mappedGoodsReceive.RowGuid;

            // Configura el formulario de eliminación usando el RowGuid del GoodsReceive creado
            _goodsReceiveFormModel.GoodsReceiveForm = new GoodsReceiveFormModel.GoodsReceiveModel
            {
                RowGuid = createdRowGuid
            };

            // Cambia el Request.Query["action"] a "delete" para simular la acción de eliminación
            _goodsReceiveFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "delete" }
            });

            // Act 
            var deleteResult = await _goodsReceiveFormModel.OnPostAsync(_goodsReceiveFormModel.GoodsReceiveForm);

            // Verifica si el resultado es de tipo RedirectResult y obtiene la URL
            var redirectResult = deleteResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define la URL esperada de redirección después de eliminar
            string expectedUrl = $"./GoodsReceiveList";
            string actualUrl = redirectResult.Url;

            // Assert
            Assert.AreEqual("Success delete existing data.", _goodsReceiveFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success delete existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            // Verifica que el registro haya sido eliminado de la BD
            var deletedGoodsReceive = await _dbContext.GoodsReceive.SingleOrDefaultAsync(gr => gr.RowGuid == createdRowGuid);
            Assert.IsNotNull(deletedGoodsReceive, "El registro debe existir en la base de datos.");
            Assert.IsFalse(deletedGoodsReceive.IsNotDeleted, "El registro debe estar marcado como eliminado (IsNotDeleted = false).");
        }


        [Test]
        public async Task OnPostAsync_ModeloInvalido_MostrarMensajeFaltaPurchaseOrderId()
        {
            // Arrange
            var invalidGoodsReceiveModel = new GoodsReceiveFormModel.GoodsReceiveModel
            {
                PurchaseOrderId = 0,
                ReceiveDate = DateTime.Now,
                Status = GoodsReceiveStatus.Confirmed,
                Description = "Descripción de prueba"
            };

            _goodsReceiveFormModel.GoodsReceiveForm = invalidGoodsReceiveModel;
            _goodsReceiveFormModel.ModelState.AddModelError("PurchaseOrderId", "The PurchaseOrderId field is required.");

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(async () => await _goodsReceiveFormModel.OnPostAsync(invalidGoodsReceiveModel));
            Assert.IsNotNull(ex);
            Assert.IsTrue(ex.Message.Contains("The PurchaseOrderId field is required."), $"Expected error message: 'The PurchaseOrderId field is required.' but got: '{ex.Message}'");
        }


    }
}
