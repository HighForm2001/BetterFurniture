// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
  

function addToCart(btn) {
    // Get the itemId from the button's data attribute
    var itemId =btn.getAttribute("data-itemId");
    // Make an AJAX call to the server to add the item to the cart
    $.ajax({
        type: "POST",
        url: "/Cart/AddToCart",
        data: { itemName: itemId },
        success: function (data) {
            // Show a pop up notification to the user
            if (data.isAuthenticated) {
                alert("The following furniture has been added to your cart:\n" + itemId);
            } else {
                alert("You need to log in to add items to the cart.");
            }
            
        },
        error: function (xhr, status, error) {
            // Handle errors here
            console.log(xhr.responseText);
            console.log(status);
            console.log(error);
        }
    });
}

function searchBox() {
    var searchBox = document.querySelector(".searchBox");
    searchBox.classList.add('active');
}
function closeBox() {
    var searchBox = document.querySelector(".searchBox");
    searchBox.classList.remove('active');
}