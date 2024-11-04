using AutoMapper;
using Indotalent.Applications.InventoryTransactions;
using Indotalent.Applications.NumberSequences;
using Indotalent.Applications.Products;
using Indotalent.Applications.TransferOuts;
using Indotalent.Applications.Warehouses;
using Indotalent.Data;
using Indotalent.Infrastructures.Repositories;
using Indotalent.Models.Entities;
using Indotalent.Models.Enums;
using Indotalent.Pages.TransferOuts;
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
    public class TransferOutTests
    {
        private Mock<IMapper> _mapperMock;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private ApplicationDbContext _dbContext;
        private NumberSequenceService _numberSequenceService;
        private TransferOutFormModel _transferOutFormModel;

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

            var warehouseService = new WarehouseService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            var productService = new ProductService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            var transferOutService = new TransferOutService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            var inventoryTransactionService = new InventoryTransactionService(warehouseService, _dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            _transferOutFormModel = new TransferOutFormModel(
                _mapperMock.Object,
                transferOutService,
                _numberSequenceService,
                warehouseService,
                productService,
                inventoryTransactionService
            );

            _transferOutFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            _transferOutFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }

        [Test]
        public async Task OnPostAsync_AgregarNuevaTransferOut()
        {
            // Arrange
            var newTransferOutModel = new TransferOutFormModel.TransferOutModel
            {
                WarehouseFromId = 1,
                WarehouseToId = 2,
                TransferReleaseDate = DateTime.Now,
                Status = TransferStatus.Confirmed,
                Description = "Descripción test"
            };

            _transferOutFormModel.TransferOutForm = newTransferOutModel;
            _transferOutFormModel.Action = "create";

            var mappedTransferOut = new TransferOut
            {
                RowGuid = Guid.NewGuid(),
                WarehouseFromId = newTransferOutModel.WarehouseFromId,
                WarehouseToId = newTransferOutModel.WarehouseToId,
                TransferReleaseDate = newTransferOutModel.TransferReleaseDate,
                Status = newTransferOutModel.Status,
                Description = newTransferOutModel.Description
            };

            // Configura el mock 
            _mapperMock.Setup(mapper => mapper.Map<TransferOut>(newTransferOutModel)).Returns(mappedTransferOut);

            _transferOutFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _transferOutFormModel.OnPostAsync(newTransferOutModel);

            // Verifica si el resultado es de tipo RedirectResult y obtiene la URL
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define la URL esperada de redirección
            string expectedUrl = $"./TransferOutForm?rowGuid={mappedTransferOut.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success create new data.", _transferOutFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            // Verifica que el número generado cumple con el formato esperado
            var expectedPrefix = "OUT";
            var expectedDate = DateTime.Now.ToString("yyyyMMdd");

            Console.WriteLine("TransferOut Number: " + mappedTransferOut.Number);

            Assert.IsTrue(mappedTransferOut.Number.EndsWith(expectedPrefix), $"El prefijo esperado es '{expectedPrefix}' pero se obtuvo '{mappedTransferOut.Number.Substring(mappedTransferOut.Number.Length - 3)}'");
            Assert.IsTrue(mappedTransferOut.Number.Contains(expectedDate), $"La fecha esperada es '{expectedDate}' pero no se encontró en '{mappedTransferOut.Number}'");

            // Verifica que el nuevo registro exista en la BD
            var createdTransferOut = await _dbContext.TransferOut
                .FirstOrDefaultAsync(to => to.RowGuid == mappedTransferOut.RowGuid);
            Assert.IsNotNull(createdTransferOut, "El registro debe existir en la BD.");
        }

        [Test]
        public async Task OnPostAsync_EditarTransferOut()
        {
            // Arrange 
            var newTransferOutModel = new TransferOutFormModel.TransferOutModel
            {
                WarehouseFromId = 1,
                WarehouseToId = 2,
                TransferReleaseDate = DateTime.Now,
                Status = TransferStatus.Confirmed,
                Description = "Descripción test"
            };

            _transferOutFormModel.TransferOutForm = newTransferOutModel;

            // Simula la acción de creación en la URL
            _transferOutFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "create" }
            });

            var mappedTransferOut = new TransferOut
            {
                RowGuid = Guid.NewGuid(),
                WarehouseFromId = newTransferOutModel.WarehouseFromId,
                WarehouseToId = newTransferOutModel.WarehouseToId,
                TransferReleaseDate = newTransferOutModel.TransferReleaseDate,
                Status = newTransferOutModel.Status,
                Description = newTransferOutModel.Description
            };

            _mapperMock.Setup(mapper => mapper.Map<TransferOut>(newTransferOutModel)).Returns(mappedTransferOut);
            _transferOutFormModel.TempData["StatusMessage"] = string.Empty;

            // Act 1 
            var createResult = await _transferOutFormModel.OnPostAsync(newTransferOutModel);
            var createdRowGuid = mappedTransferOut.RowGuid;

            // Arrange
            var editedTransferOutModel = new TransferOutFormModel.TransferOutModel
            {
                RowGuid = createdRowGuid,
                WarehouseFromId = 1,
                WarehouseToId = 3, 
                TransferReleaseDate = DateTime.Now.AddDays(1), 
                Status = TransferStatus.Cancelled, 
                Description = "Descripción actualizada"
            };

            _transferOutFormModel.TransferOutForm = editedTransferOutModel;

            // Cambia el Request.Query["action"] a "edit" para simular la acción de edición
            _transferOutFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "edit" }
            });

            // Configura el 
            _mapperMock.Setup(mapper => mapper.Map(editedTransferOutModel, mappedTransferOut))
                       .Callback((TransferOutFormModel.TransferOutModel source, TransferOut destination) =>
                       {
                           destination.TransferReleaseDate = source.TransferReleaseDate;
                           destination.Status = source.Status;
                           destination.Description = source.Description;
                           destination.WarehouseToId = source.WarehouseToId;
                       });

            // Act 2 
            var editResult = await _transferOutFormModel.OnPostAsync(editedTransferOutModel);

            // Verifica si el resultado es de tipo RedirectResult y obtiene la URL
            var redirectResult = editResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define la URL esperada de redirección
            string expectedUrl = $"./TransferOutForm?rowGuid={editedTransferOutModel.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;

            // Assert
            Assert.AreEqual("Success update existing data.", _transferOutFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success update existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            // Verifica que los valores actualizados del registro existan en la base de datos
            var updatedTransferOut = await _dbContext.TransferOut
                .FirstOrDefaultAsync(to => to.RowGuid == editedTransferOutModel.RowGuid);

            Assert.IsNotNull(updatedTransferOut, "El registro actualizado debe existir en la BD.");
            Assert.AreEqual(editedTransferOutModel.TransferReleaseDate, updatedTransferOut.TransferReleaseDate, "Campo TransferReleaseDate actualizado correctamente.");
            Assert.AreEqual(editedTransferOutModel.Status, updatedTransferOut.Status, "Campo Status actualizado correctamente.");
            Assert.AreEqual(editedTransferOutModel.Description, updatedTransferOut.Description, "Campo Description actualizado correctamente.");
            Assert.AreEqual(editedTransferOutModel.WarehouseToId, updatedTransferOut.WarehouseToId, "Campo WarehouseToId actualizado correctamente.");
        }




        [Test]
        public async Task OnPostAsync_EliminarTransferOut()
        {
            // Arrange
            var newTransferOutModel = new TransferOutFormModel.TransferOutModel
            {
                WarehouseFromId = 1,
                WarehouseToId = 2,
                TransferReleaseDate = DateTime.Now,
                Status = TransferStatus.Confirmed,
                Description = "Descripción test"
            };

            _transferOutFormModel.TransferOutForm = newTransferOutModel;

            // Crea y agrega el objeto TransferOut a la BD
            var mappedTransferOut = new TransferOut
            {
                RowGuid = Guid.NewGuid(),
                WarehouseFromId = newTransferOutModel.WarehouseFromId,
                WarehouseToId = newTransferOutModel.WarehouseToId,
                TransferReleaseDate = newTransferOutModel.TransferReleaseDate,
                Status = newTransferOutModel.Status,
                Description = newTransferOutModel.Description
            };

            await _dbContext.TransferOut.AddAsync(mappedTransferOut);
            await _dbContext.SaveChangesAsync();

            // Guarda el RowGuid del TransferOut recién creado para usarlo en la eliminación
            var createdRowGuid = mappedTransferOut.RowGuid;

            // Configura el formulario de eliminación usando el RowGuid del TransferOut creado
            _transferOutFormModel.TransferOutForm = new TransferOutFormModel.TransferOutModel
            {
                RowGuid = createdRowGuid
            };

            // Cambia el Request.Query["action"] a "delete" para simular la acción de eliminación
            _transferOutFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "delete" }
            });

            // Act 
            var deleteResult = await _transferOutFormModel.OnPostAsync(_transferOutFormModel.TransferOutForm);

            // Verifica si el resultado es de tipo RedirectResult y obtiene la URL
            var redirectResult = deleteResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define la URL esperada de redirección después de eliminar
            string expectedUrl = $"./TransferOutList";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success delete existing data.", _transferOutFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success delete existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            // Verifica que el registro haya sido eliminado de la BD
            var deletedTransferOut = await _dbContext.TransferOut.SingleOrDefaultAsync(to => to.RowGuid == createdRowGuid);
            Assert.IsNotNull(deletedTransferOut, "El registro debe existir en la base de datos.");
            Assert.IsFalse(deletedTransferOut.IsNotDeleted, "El registro debe estar marcado como eliminado (IsNotDeleted = false).");
        }


    }
}
