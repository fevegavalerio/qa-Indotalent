using AutoMapper;
using Indotalent.Applications.Taxes;
using Indotalent.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace TestProject1
{
    [TestFixture]
    public class TaxFormModelTests
    {
        private Mock<IMapper> _mapperMock;
        private Mock<TaxService> _taxServiceMock;
        private TaxFormModel _taxFormModel;

        [SetUp]
        public void SetUp()
        {
            _mapperMock = new Mock<IMapper>();
            _taxServiceMock = new Mock<TaxService>();
            _taxFormModel = new TaxFormModel(_mapperMock.Object, _taxServiceMock.Object);
        }

        // 1. Test: Carga de datos existentes: Verifica que el método OnGetAsync cargue correctamente un impuesto existente.
        [Test]
        public async Task OnGetAsync_LoadsExistingTaxData()
        {
            // Arrange
            var rowGuid = Guid.NewGuid();
            var existingTax = new Tax { RowGuid = rowGuid, Name = "Tax1", Description = "Description1", Percentage = 15.0 };
            _taxServiceMock.Setup(service => service.GetByRowGuidAsync(rowGuid)).ReturnsAsync(existingTax);
            _mapperMock.Setup(m => m.Map<TaxFormModel.TaxModel>(existingTax)).Returns(new TaxFormModel.TaxModel { RowGuid = rowGuid, Name = "Tax1", Description = "Description1", Percentage = 15.0 });

            // Act
            await _taxFormModel.OnGetAsync(rowGuid);

            // Assert
            Assert.NotNull(_taxFormModel.TaxForm);
            Assert.AreEqual("Tax1", _taxFormModel.TaxForm.Name);
        }

        // 2. Test: Modelo nuevo: Verifica que se cree un nuevo modelo de impuesto si rowGuid es nulo.
        [Test]
        public async Task OnGetAsync_CreatesNewTaxModel_IfRowGuidIsNull()
        {
            // Arrange
            // No existing tax data

            // Act
            await _taxFormModel.OnGetAsync(null);

            // Assert
            Assert.NotNull(_taxFormModel.TaxForm);
            Assert.AreEqual(Guid.Empty, _taxFormModel.TaxForm.RowGuid);
        }

        // 3. Test: Crear entrada de impuesto: Asegura que OnPostAsync crea una nueva entrada de impuesto.
        [Test]
        public async Task OnPostAsync_CreatesNewTaxEntry()
        {
            // Arrange
            var taxModel = new TaxFormModel.TaxModel { RowGuid = Guid.NewGuid(), Name = "New Tax", Description = "New Tax Description", Percentage = 10.0 };
            _taxFormModel.TaxForm = taxModel;

            // Act
            var result = await _taxFormModel.OnPostAsync(taxModel);

            // Assert
            _taxServiceMock.Verify(service => service.AddAsync(It.IsAny<Tax>()), Times.Once);
            Assert.IsInstanceOf<RedirectToPageResult>(result);
        }

        // 4. Test: Actualizar entrada de impuesto: Verifica que OnPostAsync actualiza correctamente una entrada de impuesto existente.
        [Test]
        public async Task OnPostAsync_UpdatesExistingTaxEntry()
        {
            // Arrange
            var rowGuid = Guid.NewGuid();
            var existingTax = new Tax { RowGuid = rowGuid, Name = "Old Tax", Description = "Old Tax Description", Percentage = 10.0 };
            var updatedTaxModel = new TaxFormModel.TaxModel { RowGuid = rowGuid, Name = "Updated Tax", Description = "Updated Tax Description", Percentage = 15.0 };
            _taxServiceMock.Setup(service => service.GetByRowGuidAsync(rowGuid)).ReturnsAsync(existingTax);
            _mapperMock.Setup(m => m.Map(updatedTaxModel, existingTax)).Callback<TaxModel, Tax>((source, dest) => { dest.Name = source.Name; dest.Description = source.Description; dest.Percentage = source.Percentage; });

            _taxFormModel.TaxForm = updatedTaxModel;

            // Act
            var result = await _taxFormModel.OnPostAsync(updatedTaxModel);

            // Assert
            _taxServiceMock.Verify(service => service.UpdateAsync(existingTax), Times.Once);
            Assert.IsInstanceOf<RedirectToPageResult>(result);
        }

        // 5. Test: Eliminar entrada de impuesto: Asegura que OnPostAsync elimina una entrada de impuesto.
        [Test]
        public async Task OnPostAsync_DeletesTaxEntry()
        {
            // Arrange
            var rowGuid = Guid.NewGuid();
            var existingTax = new Tax { RowGuid = rowGuid };
            _taxServiceMock.Setup(service => service.GetByRowGuidAsync(rowGuid)).ReturnsAsync(existingTax);

            var deleteModel = new TaxFormModel.TaxModel { RowGuid = rowGuid };

            // Act
            var result = await _taxFormModel.OnPostAsync(deleteModel);

            // Assert
            _taxServiceMock.Verify(service => service.DeleteByRowGuidAsync(rowGuid), Times.Once);
            Assert.IsInstanceOf<RedirectToPageResult>(result);
        }

        // 6. Test: Estado del modelo inválido: Comprueba que si el estado del modelo es inválido, OnPostAsync retorna la misma página.
        [Test]
        public async Task OnPostAsync_ReturnsPage_IfModelStateIsInvalid()
        {
            // Arrange
            _taxFormModel.ModelState.AddModelError("Name", "Required");

            // Act
            var result = await _taxFormModel.OnPostAsync(new TaxFormModel.TaxModel());

            // Assert
            Assert.IsInstanceOf<PageResult>(result);
        }

        // 7. Test: Excepción al cargar datos existentes: Asegura que se lance una excepción si no se pueden cargar datos existentes para actualizar
        [Test]
        public async Task OnPostAsync_ThrowsException_IfUnableToLoadExistingData()
        {
            // Arrange
            var rowGuid = Guid.NewGuid();
            var updateModel = new TaxFormModel.TaxModel { RowGuid = rowGuid };
            _taxServiceMock.Setup(service => service.GetByRowGuidAsync(rowGuid)).ReturnsAsync((Tax)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<Exception>(async () => await _taxFormModel.OnPostAsync(updateModel));
            Assert.That(exception.Message, Is.EqualTo("Cannot find existing tax data."));
        }

        // 8. Test: Excepción al eliminar: Verifica que se lance una excepción si no se pueden cargar datos existentes para eliminar.
        [Test]
        public async Task OnPostAsync_ThrowsException_IfUnableToLoadExistingDataForDelete()
        {
            // Arrange
            var rowGuid = Guid.NewGuid();
            var deleteModel = new TaxFormModel.TaxModel { RowGuid = rowGuid };
            _taxServiceMock.Setup(service => service.GetByRowGuidAsync(rowGuid)).ReturnsAsync((Tax)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<Exception>(async () => await _taxFormModel.OnPostAsync(deleteModel));
            Assert.That(exception.Message, Is.EqualTo("Cannot find existing tax data."));
        }

        // 9. Test: Excepción por estado de modelo inválido en crear: Asegura que se lance una excepción si el modelo es inválido al intentar crear un nuevo impuesto.
        [Test]
        public async Task OnPostAsync_ThrowsException_IfModelStateIsInvalidAndActionIsCreate()
        {
            // Arrange
            _taxFormModel.ModelState.AddModelError("Name", "Required");
            _taxFormModel.TaxForm = new TaxFormModel.TaxModel();

            // Act & Assert
            var exception = Assert.ThrowsAsync<Exception>(async () => await _taxFormModel.OnPostAsync(new TaxFormModel.TaxModel()));
            Assert.That(exception.Message, Is.EqualTo("Model state is invalid."));
        }

        // 10. Test: Redirección a TaxList al eliminar con éxito: Comprueba que se redirija a la lista de impuestos después de una eliminación exitosa.
        [Test]
        public async Task OnPostAsync_RedirectsToTaxList_OnDeleteSuccess()
        {
            // Arrange
            var rowGuid = Guid.NewGuid();
            var existingTax = new Tax { RowGuid = rowGuid };
            _taxServiceMock.Setup(service => service.GetByRowGuidAsync(rowGuid)).ReturnsAsync(existingTax);

            var deleteModel = new TaxFormModel.TaxModel { RowGuid = rowGuid };

            // Act
            var result = await _taxFormModel.OnPostAsync(deleteModel);

            // Assert
            Assert.IsInstanceOf<RedirectToPageResult>(result);
            Assert.AreEqual("./TaxList", ((RedirectToPageResult)result).PageName);
        }

        // 11. Test: Estado del mensaje: Verifica que se establece correctamente el mensaje de estado al cargar el impuesto.
        [Test]
        public async Task OnGetAsync_SetsStatusMessage()
        {
            // Arrange
            var rowGuid = Guid.NewGuid();
            var existingTax = new Tax { RowGuid = rowGuid, Name = "Tax1" };
            _taxServiceMock.Setup(service => service.GetByRowGuidAsync(rowGuid)).ReturnsAsync(existingTax);
            _mapperMock.Setup(m => m.Map<TaxFormModel.TaxModel>(existingTax)).Returns(new TaxFormModel.TaxModel { RowGuid = rowGuid, Name = "Tax1" });

            // Act
            await _taxFormModel.OnGetAsync(rowGuid);

            // Assert
            Assert.That(_taxFormModel.StatusMessage, Is.EqualTo("Tax loaded successfully."));
        }

        // 12. Test: Mensaje de error: Verifica que se establece un mensaje de error si no se encuentra el impuesto.
        [Test]
        public async Task OnGetAsync_SetsErrorMessage_IfTaxNotFound()
        {
            // Arrange
            var rowGuid = Guid.NewGuid();
            _taxServiceMock.Setup(service => service.GetByRowGuidAsync(rowGuid)).ReturnsAsync((Tax)null);

            // Act
            await _taxFormModel.OnGetAsync(rowGuid);

            // Assert
            Assert.That(_taxFormModel.StatusMessage, Is.EqualTo("Tax not found."));
        }

        // 13. Test: Mensaje de advertencia: Comprueba que se establece un mensaje de advertencia si se intenta acceder a un impuesto inexistente.
        [Test]
        public async Task OnGetAsync_SetsWarningMessage_IfTaxDoesNotExist()
        {
            // Arrange
            var rowGuid = Guid.NewGuid();
            _taxServiceMock.Setup(service => service.GetByRowGuidAsync(rowGuid)).ReturnsAsync((Tax)null);

            // Act
            await _taxFormModel.OnGetAsync(rowGuid);

            // Assert
            Assert.That(_taxFormModel.StatusMessage, Is.EqualTo("No tax entry found with the provided RowGuid."));
        }
    }
}
