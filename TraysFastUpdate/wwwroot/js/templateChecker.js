// Template Check Helper
// Place this file in wwwroot/js/ if you want to use it

function checkTemplateFiles() {
    const templates = [
        'ReportMacroTemplate_MV.docx',
        'ReportMacroTemplate_Space.docx'
    ];
    
    console.log('Expected template files in wwwroot:');
    templates.forEach(template => {
        console.log(`- ${template}`);
    });
    
    console.log('\nTemplate selection logic:');
    console.log('- Type A (Pink color) for MV cables ? ReportMacroTemplate_MV.docx');
    console.log('- All other tray purposes ? ReportMacroTemplate_Space.docx');
}

// Call this function in browser console to see template requirements
checkTemplateFiles();