using AutoMapper;
using Indotalent.Applications.ProductGroups;
using Indotalent.Applications.UnitMeasures;
using Indotalent.Data;
using Indotalent.Infrastructures.Repositories;
using Indotalent.Models.Entities;
using Indotalent.Pages.ProductGroups;
using Indotalent.Pages.UnitMeasures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using OpenQA.Selenium.BiDi.Modules.BrowsingContext;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace TestProject1
{
    [TestFixture]
    public class UnitMeasureFormTests
    {
        private Mock<IMapper> _mapperMock;
        private UnitMeasureService _unitMeasureService;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private UnitMeasureFormModel _unitMeasureFormModel;
        private ApplicationDbContext _dbContext;

        [SetUp]
        public void Setup()
        {
            // Crear mocks de las dependencias
            _mapperMock = new Mock<IMapper>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _auditColumnTransformerMock = new Mock<IAuditColumnTransformer>();

            // Configurar DbContextOptions para ApplicationDbContext en modo de pruebas
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            // Crear y asignar la instancia de ApplicationDbContext
            _dbContext = new ApplicationDbContext(options);

            // Crear el servicio utilizando _dbContext real y los mocks de otras dependencias
            _unitMeasureService = new UnitMeasureService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            // Crear la instancia del modelo de formulario utilizando el servicio real
            _unitMeasureFormModel = new UnitMeasureFormModel(_mapperMock.Object, _unitMeasureService);

            // Configurar TempData para evitar errores de referencia nula
            _unitMeasureFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Configurar HttpContext en PageContext directamente
            _unitMeasureFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }




        [Test]
        public async Task OnPostAsync_AgregarNuevaUnitMeasure()
        {
            // Arrange
            var newUnitMeasureModel = new UnitMeasureFormModel.UnitMeasureModel
            {
                Name = "liter",
                Description = "Unidad de volumen"
            };

            _unitMeasureFormModel.UnitMeasureForm = newUnitMeasureModel;
            _unitMeasureFormModel.Action = "create";

            var mappedUnitMeasure = new UnitMeasure
            {
                RowGuid = Guid.NewGuid(),
                Name = newUnitMeasureModel.Name,
                Description = newUnitMeasureModel.Description
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newUnitMeasureModel;
            var expectedMappedResult = mappedUnitMeasure;

            // Configura el mock para devolver el objeto esperado cuando se llame al método Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<UnitMeasure>(sourceModel))
                .Returns(expectedMappedResult);

            _unitMeasureFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _unitMeasureFormModel.OnPostAsync(newUnitMeasureModel);

            // Verifica si el `createResult` es de tipo RedirectResult y obtiene la URL
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define los valores esperados y obtenidos
            string expectedUrl = $"./UnitMeasureForm?rowGuid={mappedUnitMeasure.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;


            // Assert
            Assert.AreEqual("Success create new data.", _unitMeasureFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");

            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }




        [Test]
        public async Task OnPostAsync_EditarUnitMeasure()
        {
            // Arrange 
            var newUnitMeasureModel = new UnitMeasureFormModel.UnitMeasureModel
            {
                Name = "liter",
                Description = "Unidad de volumen"
            };

            _unitMeasureFormModel.UnitMeasureForm = newUnitMeasureModel;

            
            _unitMeasureFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "create" }
            });

            var mappedUnitMeasure = new UnitMeasure
            {
                RowGuid = Guid.NewGuid(),
                Name = newUnitMeasureModel.Name,
                Description = newUnitMeasureModel.Description
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newUnitMeasureModel;
            var expectedMappedResult = mappedUnitMeasure;

            // Configura el mock para devolver el objeto esperado cuando se llame al método Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<UnitMeasure>(sourceModel))
                .Returns(expectedMappedResult);

            _unitMeasureFormModel.TempData["StatusMessage"] = string.Empty;

            // Act 1
            var createResult = await _unitMeasureFormModel.OnPostAsync(newUnitMeasureModel);
           
            var createdRowGuid = mappedUnitMeasure.RowGuid;

            // Arrange
            var editedUnitMeasureModel = new UnitMeasureFormModel.UnitMeasureModel
            {
                RowGuid = createdRowGuid,
                Name = "liter",
                Description = "unit of volume"
            };

            _unitMeasureFormModel.UnitMeasureForm = editedUnitMeasureModel;

            // Cambia el `Request.Query["action"]` a "edit" para simular edición
            _unitMeasureFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "edit" }
            });

            // Configura el mock para mapear el modelo de entrada editado a la entidad
            _mapperMock
                .Setup(mapper => mapper.Map(editedUnitMeasureModel, mappedUnitMeasure))
                .Callback((UnitMeasureFormModel.UnitMeasureModel source, UnitMeasure destination) =>
                {
                    destination.Name = source.Name;
                    destination.Description = source.Description;
                });

            // Act 2
            var editResult = await _unitMeasureFormModel.OnPostAsync(editedUnitMeasureModel);

            // Verifica si el `editResult` es de tipo RedirectResult y obtén la URL
            var redirectResult = editResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");


            string expectedUrl = $"./UnitMeasureForm?rowGuid={editedUnitMeasureModel.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success update existing data.", _unitMeasureFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success update existing data.");

            Assert.AreEqual("liter", mappedUnitMeasure.Name, "Campo Name Actualizado.");
            Assert.AreEqual("unit of volume", mappedUnitMeasure.Description, "Campo Description Actualizado.");

            // Usa Assert.AreEqual para un mensaje de error detallado
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

        }


        [Test]
        public async Task OnPostAsync_EliminarUnitMeasure()
        {
            // Arrange 
            var newUnitMeasureModel = new UnitMeasureFormModel.UnitMeasureModel
            {
                Name = "liter",
                Description = "Unidad de volumen"
            };

            _unitMeasureFormModel.UnitMeasureForm = newUnitMeasureModel;

          
            var mappedUnitMeasure = new UnitMeasure
            {
                RowGuid = Guid.NewGuid(),
                Name = newUnitMeasureModel.Name,
                Description = newUnitMeasureModel.Description,
                IsNotDeleted = true 
            };

            // Act 1
            await _dbContext.UnitMeasure.AddAsync(mappedUnitMeasure);
            await _dbContext.SaveChangesAsync();

            _unitMeasureFormModel.TempData["StatusMessage"] = string.Empty;

            // Act 2
            _unitMeasureFormModel.UnitMeasureForm = new UnitMeasureFormModel.UnitMeasureModel
            {
                RowGuid = mappedUnitMeasure.RowGuid
            };

            // Cambia el `Request.Query["action"]` a "delete" para simular la acción de eliminación
            _unitMeasureFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "delete" }
            });

            var deleteResult = await _unitMeasureFormModel.OnPostAsync(_unitMeasureFormModel.UnitMeasureForm);

            // Verifica que el resultado de `deleteResult` sea de tipo RedirectResult y obtiene la URL
            var redirectResult = deleteResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");

            // Define la URL esperada y la URL obtenida
            string expectedUrl = $"./UnitMeasureList";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success delete existing data.", _unitMeasureFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success delete existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            // Verifica que el registro se haya marcado como eliminado en la base de datos
            var deletedUnitMeasure = await _dbContext.UnitMeasure
                .SingleOrDefaultAsync(u => u.RowGuid == mappedUnitMeasure.RowGuid);


            Assert.IsNotNull(deletedUnitMeasure, "El registro debe existir en la base de datos.");
            Assert.IsFalse(deletedUnitMeasure.IsNotDeleted, "El registro debe estar marcado como eliminado (IsNotDeleted = false).");
        }






    }







}
