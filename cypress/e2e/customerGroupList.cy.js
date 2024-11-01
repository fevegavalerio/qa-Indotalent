describe('Pruebas de pagina Customer Groups', () => {

    it('Debería permitir descargar el archivo Excel para Customer Groups', () => {
        // Visita la pagina que queremos probar
        cy.visit('https://whms-lte.csharpasp.net/CustomerGroups/CustomerGroupList'); 

        // Hacer clic en el primer botón que coincide
        cy.get('.btn.btn-primary.btn-block').first().click();

        // Espera a que el grid se cargue
        cy.get('#Grid').should('exist');

        // Intercepta la solicitud de exportación
        cy.intercept('POST', '/odata/CustomerGroup/**').as('excelExport');

        // Simula un clic en el botón de exportación de Excel
        cy.get('#Grid_excelexport').click();

        // Espera a que se complete la solicitud de exportación
        cy.wait(4000);

        const downloadsFolder = Cypress.config('downloadsFolder'); 
        cy.readFile(`${downloadsFolder}/Export.xlsx`).should('exist'); 
    });
    
    it('Debería permitir añadir un nuevo elemento a Customer Groups', () => {
        //Visita la pagina que queremos probar
        cy.visit('https://whms-lte.csharpasp.net/CustomerGroups/CustomerGroupForm?action=create');

        // Hacer clic en el primer botón que coincide
        cy.get('.btn.btn-primary.btn-block').first().click();

        // Completa el campo "Name"
        cy.get('#CustomerGroupForm_Name').type('Nuevo Grupo de Clientes'); 

        // Completa el campo "Description"
        cy.get('#CustomerGroupForm_Description').type('Descripción del nuevo grupo de clientes.');

        // Envía el formulario
        cy.get('#btnSubmit').click();

        // Refresca pagina
        cy.visit('https://whms-lte.csharpasp.net/CustomerGroups/CustomerGroupList'); 

        // Espera a que el grid se cargue
        cy.get('#Grid').should('exist'); 

        // Verifica que el nuevo elemento aparezca en el grid
        cy.get('#Grid').contains('Nuevo Grupo de Clientes').should('exist');
    });
});