// ===== Color Picker =====
document.getElementById("colorPick")?.addEventListener("change", function () {
    const color = this.value;
    document.documentElement.style.setProperty("--theme-color", color);
    document.documentElement.style.setProperty("--theme-color-rgb", color);
    localStorage.setItem("theme-color", color);
});

// ===== Dark / Light Toggle =====
$("#darkButton").on("click", function () {
    document.documentElement.classList.remove("light");
    document.documentElement.classList.add("dark");
    document.getElementById("color-link").setAttribute("href", "/css/dark.css");
    localStorage.setItem("theme", "dark");
});

$("#lightButton").on("click", function () {
    document.documentElement.classList.remove("dark");
    document.documentElement.classList.add("light");
    document.getElementById("color-link").setAttribute("href", "/css/style.css");
    localStorage.setItem("theme", "light");
});

// ===== RTL / LTR Toggle =====
$(".theme-setting-button.rtl").on("click", "button", function () {
    const isRTL = $(this).text().trim().toLowerCase() === "rtl";
    if (isRTL) {
        document.documentElement.setAttribute("dir", "rtl");
        $("#rtl-link").attr("href", "/css/vendors/bootstrap.rtl.css");
        localStorage.setItem("direction", "rtl");
    } else {
        document.documentElement.setAttribute("dir", "ltr");
        $("#rtl-link").attr("href", "/css/vendors/bootstrap.css");
        localStorage.setItem("direction", "ltr");
    }
});

window.addEventListener("DOMContentLoaded", function () {
    const savedColor = localStorage.getItem("theme-color");
    const input = document.getElementById("colorPick");
    if (savedColor && input) input.value = savedColor;
});
