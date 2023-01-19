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
            alert("The following furniture has been added to your cart:\n"+itemId);
        },
        error: function (xhr, status, error) {
            // Handle errors here
            console.log(xhr.responseText);
            console.log(status);
            console.log(error);
        }
    });
}

/*console.log("Javascript is running!")
document.getElementById("addToCartBtn").addEventListener("click", function (event) {
    var itemId = event.target.dataset.itemId;
    $.ajax({
        type: "POST",
        url: "/Cart/AddToCart",
        data: { itemName: itemId },
        success: function (response) {
            if (response.success) {
                $('#addToCartModal').modal('show');
            }
        }
    });
});
});*/


