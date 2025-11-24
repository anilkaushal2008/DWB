$(function () {
    "use strict";

    /* scrollar */
    new PerfectScrollbar(".notify-list");
    new PerfectScrollbar(".search-content");

    /* toggle button */
    $(".btn-toggle").click(function () {
        $("body").hasClass("toggled") ? ($("body").removeClass("toggled"), $(".sidebar-wrapper").unbind("hover")) : ($("body").addClass("toggled"), $(".sidebar-wrapper").hover(function () {
            $("body").addClass("sidebar-hovered")
        }, function () {
            $("body").removeClass("sidebar-hovered")
        }))
    });

    /* menu */
    $(function () {
        $('#sidenav').metisMenu();
    });

    $(".sidebar-close").on("click", function () {
        $("body").removeClass("toggled")
    });

    /* dark mode button */
    $(".dark-mode i").click(function () {
        $(this).text(function (i, v) {
            return v === 'dark_mode' ? 'light_mode' : 'dark_mode'
        })
    });

    $(".dark-mode").click(function () {
        $("html").attr("data-bs-theme", function (i, v) {
            return v === 'dark' ? 'light' : 'dark';
        })
    });

    /* sticky header */
    $(document).ready(function () {
        $(window).on("scroll", function () {
            if ($(this).scrollTop() > 60) {
                $('.top-header .navbar').addClass('sticky-header');
            } else {
                $('.top-header .navbar').removeClass('sticky-header');
            }
        });
    });

    /* email */
    $(".email-toggle-btn").on("click", function () {
        $(".email-wrapper").toggleClass("email-toggled")
    });
    $(".email-toggle-btn-mobile").on("click", function () {
        $(".email-wrapper").removeClass("email-toggled")
    });
    $(".compose-mail-btn").on("click", function () {
        $(".compose-mail-popup").show()
    });
    $(".compose-mail-close").on("click", function () {
        $(".compose-mail-popup").hide()
    });

    /* chat */
    $(".chat-toggle-btn").on("click", function () {
        $(".chat-wrapper").toggleClass("chat-toggled")
    });
    $(".chat-toggle-btn-mobile").on("click", function () {
        $(".chat-wrapper").removeClass("chat-toggled")
    });

    /* switcher with persistence */
    function setTheme(themeName) {
        $("html").attr("data-bs-theme", themeName);
        localStorage.setItem('dwb_theme', themeName); // <--- CRITICAL: SAVES TO STORAGE
    }

    $("#BlueTheme").on("click", function () { setTheme("blue-theme") });
    $("#LightTheme").on("click", function () { setTheme("light") });
    $("#DarkTheme").on("click", function () { setTheme("dark") });
    $("#SemiDarkTheme").on("click", function () { setTheme("semi-dark") });
    $("#BoderedTheme").on("click", function () { setTheme("bodered-theme") });

    /* search control */
    $(".search-control").click(function () {
        $(".search-popup").addClass("d-block");
        $(".search-close").addClass("d-block");
    });

    $(".search-close").click(function () {
        $(".search-popup").removeClass("d-block");
        $(".search-close").removeClass("d-block");
    });

    $(".mobile-search-btn").click(function () {
        $(".search-popup").addClass("d-block");
    });

    $(".mobile-search-close").click(function () {
        $(".search-popup").removeClass("d-block");
    });

    /* menu active */
    $(function () {
        for (var e = window.location, o = $(".metismenu li a").filter(function () {
            return this.href == e
        }).addClass("").parent().addClass("mm-active"); o.is("li");) o = o.parent("").addClass("mm-show").parent("").addClass("mm-active")
    });


    /* Modern Card Styling */
.card {
    border: none!important; /* Remove hard grey borders */
    border - radius: 1rem!important; /* Softer corners (16px) */
    box - shadow: 0 10px 30px - 12px rgba(0, 0, 0, 0.1); /* Soft, floating shadow */
    transition: transform 0.2s ease, box - shadow 0.2s ease;
}

/* Hover Effect for interactivity */
.card:hover {
    transform: translateY(-3px); /* Slight lift */
    box - shadow: 0 15px 35px - 10px rgba(0, 0, 0, 0.15);
}

/* Header cleanup */
.card - header {
    background - color: transparent;
    border - bottom: 1px solid rgba(0, 0, 0, 0.05);
    padding: 1.5rem;
}

.card - body {
    padding: 1.5rem;
    }
});