(function ($) {

    "use strict";
    jQuery(window).on('load', function () {

        // search filters
        $('.reset-filter').on('click', 'button', function (e) {
            e.preventDefault();
            // reload url without query vars
            var url = window.location.href;
            // remove the part after post_type= wp-manga
            var new_url = url.split('&post_type=wp-manga')[0] + '&post_type=wp-manga';
            // reload with new_url
            window.location.href = new_url;
        })

        $('body.search .c-search-header__wrapper .search-content .search-form .search-submit').on('click', function (e) {
            e.preventDefault();
            var url = window.location.href;
            //replace the param s with the new value
            var new_url = url.replace(/s=[^&]*/g, 's=' + $('.search-form .search-field').val());
            //reload with new_url
            window.location.href = new_url;
        })

        //off-canvas chapter lists
        const $hamburger = $(".hamburger")
        const $menuModal = $("#chaptersModal")

        $hamburger.on("click", function () {
            $hamburger.toggleClass("is-active")
        });

        $menuModal.on("hidden.bs.modal", function () {
            $hamburger.removeClass("is-active")
        })

        if ($('.nav-links .nav-previous').length > 0) {
            $('.right-footer .prev-chap').addClass('available')
        };

        if ($('.nav-links .nav-next').length > 0) {
            $('.right-footer .next-chap').addClass('available')
        };

        $('.right-footer .prev-chap').on('click', function (e) {
            var item = $('.nav-links .nav-previous a')[0];
            $(item).trigger('click');
        })

        $('.right-footer .next-chap').on('click', function (e) {
            var item = $('.nav-links .nav-next a')[0];
            $(item).trigger('click');
        })

        //extra reading settings
        $(document).on('click', '.reading-settings .open-reader-settings', function (e) {
            e.preventDefault();
            $(this).parent().toggleClass('active');
            const selected_font = window.wpmanga.getCookie('wpmanga-reading-font');
            console.log(selected_font, $('.theme-set-font input[value="' + selected_font + '"]').parent())
            $('.theme-set-font input[value="' + selected_font + '"]').parent().addClass('active');
        });

        $('#manta-reader-settings .close').on('click', function () {
            $('.reading-settings').removeClass('active')
        })

        $('#fontRange').on('change', function () {
            const fontSize = $(this).val();
            $('.reading-content .text-left').css({ 'font-size': fontSize + 'px', 'line-height': (fontSize * 1.5) + 'px' });
            // save cookie
            window.wpmanga.setCookie('wpmanga-reading-fontsize', fontSize, 30);
        })

        $('.theme-set-font input').on('click', function () {
            $('.theme-set-font li').removeClass('active');
            $(this).parent().addClass('active');
            $font = $(this).val();
            $('body').attr('data-font', $font);
            window.wpmanga.setCookie('wpmanga-reading-font', $font, 30);
        });

        $('.theme-set-color input').on('click', function () {
            $('.theme-set-color label').removeClass('active');
            $('.theme-set-color input').removeAttr('checked');
            $(this).attr('checked', 'checked');
            $(this).parent().addClass('active');
            $color = $(this).parent().data('schema');
            $('body').attr('data-schema', $color);
            if ($color !== 'dark') {
                $('body').removeClass('text-ui-light').addClass('text-ui-dark');
            } else {
                $('body').removeClass('text-ui-dark').addClass('text-ui-light');
            }
            window.wpmanga.setCookie('wpmanga-reading-schema', $color, 30);
        });

        if ($('body').hasClass('reading-manga')) {

            var $color = window.wpmanga.getCookie('wpmanga-reading-schema');
            if ($color != '') {
                $('.theme-set-color label input').removeAttr('checked');
                $('.theme-set-color').find('label._' + $color).addClass('active').find('input').attr('checked', 'checked');

                $('body').attr('data-schema', $color);
                if ($color !== 'dark') {
                    $('body').removeClass('text-ui-light').addClass('text-ui-dark');
                } else {
                    $('body').removeClass('text-ui-dark').addClass('text-ui-light');
                }
            }

            var $font = window.wpmanga.getCookie('wpmanga-reading-font');

            if ($font) {
                $('.theme-set-font label input').removeAttr('checked');
                $('.theme-set-font').find('label.' + $font).find('input').attr('checked', 'checked');

                if ($('.c-page-content.chapter-type-text').length > 0) {
                    $('body').attr('data-font', $font);
                }
            }
        }

        //shortcodes

        $(".tp-heroslider .slider__container_custom").each(function () {
            var $this = $(this);

            var autoplay = $(this).parents(".tp-heroslider").attr('data-autoplay');
            autoplay = autoplay == "1" ? true : false;
            var manga_slidesToShow = parseInt($(this).parents(".tp-heroslider").attr('data-count'));

            var check_rtl = (jQuery("body").css('direction') === "rtl");
            var args = {
                dots: false,
                infinite: true,
                speed: 500,
                centerMode: true,
                slidesToShow: manga_slidesToShow,
                slidesToScroll: 1,
                arrows: true,
                rtl: check_rtl,
                autoplay: autoplay,
                variableWidth: true,
                responsive: [{
                    breakpoint: 992,
                    settings: {
                        slidesToShow: (manga_slidesToShow == 1) ? 1 : 3,
                        slidesToScroll: 1,
                        infinite: true,
                        centerMode: false,
                        dots: false,
                        variableWidth: true,
                    }
                }, {
                    breakpoint: 660,
                    settings: {
                        slidesToShow: 3,
                        slidesToScroll: 1,
                        infinite: true,
                        variableWidth: true,
                        dots: false
                    }
                }, {
                    breakpoint: 480,
                    settings: {
                        slidesToShow: 2,
                        variableWidth: true,
                    }
                }]
            }

            $this.slick(args);

        });

        $(".manga-x-slider.style-3 .slider-for").slick({
            slidesToShow: 1,
            slidesToScroll: 1,
            arrows: false,
            asNavFor: '.slider-nav'
        })

        $('.manga-x-slider.style-3 .slider-nav').slick({
            slidesToShow: 4,
            slidesToScroll: 1,
            asNavFor: '.slider-for',
            dots: true,
            arrows: false,
            focusOnSelect: true,
            infinite: true,
            responsive: [
                {
                    breakpoint: 768,
                    settings: {
                        slidesToShow: 3,
                        slidesToScroll: 3
                    }
                },
                {
                    breakpoint: 480,
                    settings: {
                        slidesToShow: 1,
                        slidesToScroll: 1
                    }
                }
            ]
        });

        $('.manga-slider.style-1 .slider__container_custom').slick({
            dots: false,
            infinite: true,
            speed: 500,
            // centerMode: true,
            slidesToShow: 5,
            slidesToScroll: 1,
            arrows: true,
            autoplay: false,
            responsive: [{
                breakpoint: 992,
                settings: {
                    slidesToShow: 4,
                    slidesToScroll: 1,
                    infinite: true,
                    centerMode: false,
                    dots: false,
                }
            }, {
                breakpoint: 660,
                settings: {
                    slidesToShow: 3,
                    slidesToScroll: 1,
                    infinite: true,
                    dots: false
                }
            }, {
                breakpoint: 480,
                settings: {
                    slidesToShow: 2,
                }
            }]
        });

        $('.page-content-listing.item-chapters').slick({
            dots: true,
            infinite: true,
            speed: 300,
            slidesToShow: 1,
            adaptiveHeight: true,
            prevArrow: '<i class="fas fa-chevron-left"></i>',
            nextArrow: '<i class="fas fa-chevron-right"></i>',
        });

        $('#view-chapters').on('click', function (e) {
            $('#manga-chapters-sidebar').toggleClass('visible');
            e.stopPropagation();
            return false;
        });

        $(document).on("mouseup", function (e) {
            var container = $("#manga-chapters-sidebar");

            // if the target of the click isn't the container nor a descendant of the container
            if (!container.is(e.target) && container.has(e.target).length === 0) {
                container.removeClass("visible");
            }
        });

        var rankitems = $('.ranking #loop-content .page-item-detail');
        if (rankitems.length > 0) {
            for (var i = 1; i <= rankitems.length; i++) {
                if (i > 10) {
                    break;
                }
                $(rankitems[i - 1]).addClass('rank-' + i);
            }
        }

        // custom like button
        $('.single-wp-manga .profile-manga .action-buttons .right').on('click', function (e) {
            e.preventDefault();
            var $this = $(this);
            $this.toggleClass('active');
            $('body').toggleClass('active-like');
        })

        $(document).on('click', '.kodex_buttons > button', function (e) {
            e.preventDefault();
            $('.kodex_buttons + i').removeClass('unvoted voted like dislike');
            var $this = $(this);
            const action = $this.data('action');
            console.log(action)
            if ($this.hasClass('kodex_button_active')) {
                //if already has liked/disliked
                $('.kodex_buttons + i').addClass('unvoted ' + action);
            } else {
                $('.kodex_buttons + i').addClass('voted ' + action);
            }
        })

        $('.kodex_buttons > button').each(function () {
            var $this = $(this);
            const action = $this.data('action');
            if ($this.hasClass('kodex_button_active')) {
                console.log(action);
                $('.kodex_buttons + i').addClass('voted ' + action);
            }
        })

        if ($('body').hasClass('reading-manga')) {
            var lightbox_option = $('#chapter_lightbox').val();
            var viewportWidth = window.outerWidth;
            if (lightbox_option == 2 || (viewportWidth < 768 && lightbox_option == 1)) {
                $('body').removeClass('click-to-scroll');
                $('.reading-manga .reading-content').css({ padding: 0 })
                $(".chapter-images").slick({
                    infinite: false,
                    slidesToShow: 1,
                    slidesPerRow: 1,
                    swipeToSlide: true,
                    slidesToScroll: 1,
                    infinite: false,
                    dots: false
                });
            }
        }

        var viewportWidth = window.outerWidth;
        if (viewportWidth < 1008) {
            if ($('.profile-manga .sidebar-col').length) {
                $('.profile-manga .sidebar-col').prependTo('.site-footer').removeClass('sidebar-col').addClass('sidebar-col-mobile');
            }
        }
    });
})(jQuery);