using AutoMapper;
using Indotalent.Applications.NumberSequences;
using Indotalent.Applications.ProductGroups;
using Indotalent.Applications.Products;
using Indotalent.Applications.UnitMeasures;
using Indotalent.Data;
using Indotalent.Infrastructures.Repositories;
using Indotalent.Models.Entities;
using Indotalent.Pages.ProductGroups;
using Indotalent.Pages.Products;
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
    public class ProductsTests
    {
        private Mock<IMapper> _mapperMock;
        private ProductService _productService;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private ProductFormModel _productFormModel;
        private ApplicationDbContext _dbContext;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private NumberSequenceService _numberSequenceService; 

        [SetUp]
        public void Setup()
        {
            // Crea mocks de las dependencias
            _mapperMock = new Mock<IMapper>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _auditColumnTransformerMock = new Mock<IAuditColumnTransformer>();

            // Configura DbContextOptions para ApplicationDbContext en modo de pruebas
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            // Crea y asignar la instancia de ApplicationDbContext
            _dbContext = new ApplicationDbContext(options);

            // Crea la instancia real de NumberSequenceService
            _numberSequenceService = new NumberSequenceService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            // Crea el servicio utilizando _dbContext real y los mocks de otras dependencias
            _productService = new ProductService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            // Crea la instancia del modelo de formulario utilizando el servicio real
            _productFormModel = new ProductFormModel(
                _mapperMock.Object,
                _productService,
                _numberSequenceService, // Usa la instancia real aquí
                new ProductGroupService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object),
                new UnitMeasureService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object)
            );

            // Configurar TempData para evitar errores de referencia nula
            _productFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Configurar HttpContext en PageContext directamente
            _productFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }






        [Test]
        public async Task OnPostAsync_AgregarNuevoProducts()
        {
            // Arrange
            var newProductModel = new ProductFormModel.ProductModel
            {
                Name = "Nuevo Producto",
                Description = "Descripción del producto",
                ProductGroupId = 1,
                UnitMeasureId = 2,
                UnitPrice = 100.0,
                Physical = true
            };

            _productFormModel.ProductForm = newProductModel;
            _productFormModel.Action = "create";

            var mappedProduct = new Product
            {
                RowGuid = Guid.NewGuid(),
                Name = newProductModel.Name,
                Description = newProductModel.Description,
                ProductGroupId = newProductModel.ProductGroupId,
                UnitMeasureId = newProductModel.UnitMeasureId,
                UnitPrice = newProductModel.UnitPrice,
                Physical = newProductModel.Physical,
            };

            // Configura el mock
            _mapperMock.Setup(mapper => mapper.Map<Product>(newProductModel)).Returns(mappedProduct);

            _productFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _productFormModel.OnPostAsync(newProductModel);

            // Verifica si el `createResult` es de tipo RedirectResult y obtiene la URL
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define los valores esperados y obtenidos
            string expectedUrl = $"./ProductForm?rowGuid={mappedProduct.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;

            // Assert
            Assert.AreEqual("Success create new data.", _productFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            // Divide el Number generado y verifica cada parte
            var expectedPrefix = "0001";  
            var expectedDate = DateTime.Now.ToString("yyyyMMdd");
            var expectedSuffix = "ART";

            Console.WriteLine("Product Number: " + mappedProduct.Number);

            
            Assert.IsTrue(mappedProduct.Number.StartsWith(expectedPrefix), $"El prefijo esperado es '{expectedPrefix}' pero se obtuvo '{mappedProduct.Number.Substring(0, 4)}'");
            Assert.IsTrue(mappedProduct.Number.Contains(expectedDate), $"La fecha esperada es '{expectedDate}' pero no se encontró en '{mappedProduct.Number}'");
            StringAssert.EndsWith(expectedSuffix, mappedProduct.Number, "El número de producto debe terminar con 'ART'.");
        }


        [Test]
        public async Task OnPostAsync_EditarNuevoProducts()
        {
            // Arrange - Crear un nuevo producto primero
            var newProductModel = new ProductFormModel.ProductModel
            {
                Name = "Producto Original",
                Description = "Descripción original",
                ProductGroupId = 1,
                UnitMeasureId = 2,
                UnitPrice = 100.0,
                Physical = true
            };

            _productFormModel.ProductForm = newProductModel;

            _productFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "create" }
            });

            var mappedProduct = new Product
            {
                RowGuid = Guid.NewGuid(),
                Name = newProductModel.Name,
                Description = newProductModel.Description,
                ProductGroupId = newProductModel.ProductGroupId,
                UnitMeasureId = newProductModel.UnitMeasureId,
                UnitPrice = newProductModel.UnitPrice,
                Physical = newProductModel.Physical
            };

            _mapperMock
                .Setup(mapper => mapper.Map<Product>(newProductModel))
                .Returns(mappedProduct);

            _productFormModel.TempData["StatusMessage"] = string.Empty;

            // Act 1 
            var createResult = await _productFormModel.OnPostAsync(newProductModel);
            var createdRowGuid = mappedProduct.RowGuid;

            // Arrange 
            var editedProductModel = new ProductFormModel.ProductModel
            {
                RowGuid = createdRowGuid,
                Name = "Producto Editado",
                Description = "Descripción actualizada",
                ProductGroupId = 1,
                UnitMeasureId = 2,
                UnitPrice = 120.0,
                Physical = false
            };

            _productFormModel.ProductForm = editedProductModel;

            // Cambiar el Request.Query["action"] a "edit" para simular la edición
            _productFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "edit" }
            });

            // Configura el mock 
            _mapperMock
                .Setup(mapper => mapper.Map(editedProductModel, mappedProduct))
                .Callback((ProductFormModel.ProductModel source, Product destination) =>
                {
                    destination.Name = source.Name;
                    destination.Description = source.Description;
                    destination.UnitPrice = source.UnitPrice;
                    destination.Physical = source.Physical;
                });

            // Act 2 
            var editResult = await _productFormModel.OnPostAsync(editedProductModel);

            // Verifica si el `editResult` es de tipo RedirectResult y obtiene la URL
            var redirectResult = editResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");

            // Define la URL esperada de redirección
            string expectedUrl = $"./ProductForm?rowGuid={editedProductModel.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success update existing data.", _productFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success update existing data.");
            Assert.AreEqual("Producto Editado", mappedProduct.Name, "Campo Name actualizado correctamente.");
            Assert.AreEqual("Descripción actualizada", mappedProduct.Description, "Campo Description actualizado correctamente.");
            Assert.AreEqual(120.0, mappedProduct.UnitPrice, "Campo UnitPrice actualizado correctamente.");
            Assert.IsFalse(mappedProduct.Physical, "Campo Physical actualizado correctamente.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }



        [Test]
        public async Task OnPostAsync_EliminarNuevoProducts()
        {
            // Arrange
            var newProductModel = new ProductFormModel.ProductModel
            {
                Name = "Producto para eliminar",
                Description = "Descripción del producto a eliminar",
                ProductGroupId = 1,
                UnitMeasureId = 2,
                UnitPrice = 100.0,
                Physical = true
            };

            _productFormModel.ProductForm = newProductModel;

            // Crea el objeto Product que será agregado a la base de datos
            var mappedProduct = new Product
            {
                RowGuid = Guid.NewGuid(),
                Name = newProductModel.Name,
                Description = newProductModel.Description,
                ProductGroupId = newProductModel.ProductGroupId,
                UnitMeasureId = newProductModel.UnitMeasureId,
                UnitPrice = newProductModel.UnitPrice,
                Physical = newProductModel.Physical,
                IsNotDeleted = true  
            };

            // Act 1
            await _dbContext.Product.AddAsync(mappedProduct);
            await _dbContext.SaveChangesAsync();

            // Guardamos el RowGuid del producto agregado
            var createdRowGuid = mappedProduct.RowGuid;

            _productFormModel.TempData["StatusMessage"] = string.Empty;

            // Configura el formulario de eliminación usando el RowGuid del producto
            _productFormModel.ProductForm = new ProductFormModel.ProductModel
            {
                RowGuid = createdRowGuid
            };

            // Cambia el `Request.Query["action"]` a "delete" para simular la acción de eliminación
            _productFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "delete" }
            });

            // Act 2 
            var deleteResult = await _productFormModel.OnPostAsync(_productFormModel.ProductForm);

            // Verifica si el `deleteResult` es de tipo RedirectResult y obtiene la URL
            var redirectResult = deleteResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");

            // Define la URL esperada de redirección
            string expectedUrl = $"./ProductList";
            string actualUrl = redirectResult.Url;

            // Assert - Verifica que el mensaje de éxito y la URL de redirección sean correctos
            Assert.AreEqual("Success delete existing data.", _productFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success delete existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            // Verifica que el registro se haya marcado como eliminado en la base de datos
            var deletedProduct = await _dbContext.Product.SingleOrDefaultAsync(p => p.RowGuid == createdRowGuid);
            Assert.IsNotNull(deletedProduct, "El producto debe existir en la base de datos.");
            Assert.IsFalse(deletedProduct.IsNotDeleted, "El producto debe estar marcado como eliminado (IsNotDeleted = false).");
        }



        [Test]
        public async Task OnPostAsync_ModeloInvalido_MostrarMensajeFaltaName()
        {
            // Arrange
            var invalidProductModel = new ProductFormModel.ProductModel
            {
                Name = "", 
                Description = "Descripción test",
                ProductGroupId = 1,
                UnitMeasureId = 2,
                UnitPrice = 100.0,
                Physical = true
            };

            _productFormModel.ProductForm = invalidProductModel;
            _productFormModel.ModelState.AddModelError("Name", "The Name field is required.");

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(async () => await _productFormModel.OnPostAsync(invalidProductModel));
            Assert.IsNotNull(ex);
            Assert.IsTrue(ex.Message.Contains("The Name field is required."), $"Expected error message: 'The Name field is required.' but got: '{ex.Message}'");
        }



    }

}
