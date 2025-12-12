/**
 * This file handles the integration with the EkaScribe extension.
 * It listens for the 'ekascribe-complete-data' event and populates
 * a form input with the data.
 */

// Function that handles the EkaScribe event
function handleScribeData(event) {
    console.log('✅ Data received from EkaScribe:', event.detail);

    const scribeData = event.detail;

    // Convert the data object to a JSON string
    const scribeDataString = JSON.stringify(scribeData);

    // --- 1. Find the HIDDEN INPUT and set its value ---
    // This is the data you will send to your .NET controller
    const hiddenInput = document.getElementById('scribeDataInput');
    if (hiddenInput) {
        hiddenInput.value = scribeDataString;
        console.log('Updated hidden input #scribeDataInput.');
    } else {
        console.warn('Could not find #scribeDataInput element.');
    }

    // --- 2. (Optional) Find the <pre> tag and display the data ---
    // This is just for testing, so you can see the data on the page
    const outputElement = document.getElementById('scribeOutput');
    if (outputElement) {
        outputElement.textContent = JSON.stringify(scribeData, null, 2); // Pretty print
    }
}

// Wait until the DOM is fully loaded, then attach the listener
document.addEventListener('DOMContentLoaded', function () {
    console.log('🟢 Waiting for EkaScribe data event...');
    // Attach the main listener to the window
    window.addEventListener('ekascribe-complete-data', handleScribeData);
});