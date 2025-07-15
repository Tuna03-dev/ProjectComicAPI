jQuery(document).ready(function($){
	$(document).on('wp_manga_on_chapter_navlinks_click', function(){
		// clear data of previous chapter
		if(typeof chapter_preloaded_images !== 'undefined'){
			chapter_preloaded_images = [];
		}
	});
	
	$(document).on('wp_manga_chapter_redirect', function(e, obj, redirect, isPageSelect, isChapterSelect){
		if(isChapterSelect){
			var selectedOption = obj.find(':selected');
			if(selectedOption.hasClass('premium-block')){
				$('#frm-wp-manga-buy-coin').modal();
				var cl = selectedOption.attr('class');
				
				var matches = cl.match(/coin-\d+/g);
				if(matches){
					var coin = matches[0].replace('coin-', '');
					$('#frm-wp-manga-buy-coin .message-sufficient .coin').html(coin);
				}
				
				matches = cl.match(/data-chapter-\d+/g);
				if(matches){
					var chapter_id = matches[0].replace('data-chapter-', '');
					$('#frm-wp-manga-buy-coin input[name="wp-manga-chapter"]').val(chapter_id);
				}
				
				return false;
			}
		}
	});
	
	var wp_manga_premium_block_click = function(evt){
		if(!window.wp_manga_chapter_coin_just_add_premium){		
			var cl = $(evt.target).closest('.premium-block').attr('class');
			
			if(typeof cl !== 'undefined'){
				var matches = cl.match(/coin-\d+/g);
				if(matches){
					var coin = matches[0].replace('coin-', '');
					$('#frm-wp-manga-buy-coin .message-sufficient .coin').html(coin);
					
					var user_balance = $('#wp_manga_chapter_coin_user_balance').length > 0 ? $('#wp_manga_chapter_coin_user_balance').val() : 0;
					
					if(parseInt(user_balance) < parseInt(coin)) {
						$('#frm-wp-manga-buy-coin .message-lack-of-coin').removeClass('hidden');
						$('#frm-wp-manga-buy-coin .message-sufficient').addClass('hidden');
						$('#frm-wp-manga-buy-coin .btn-agree').hide();
					} else {
						$('#frm-wp-manga-buy-coin .message-lack-of-coin').addClass('hidden');
						$('#frm-wp-manga-buy-coin .message-sufficient').removeClass('hidden');
						$('#frm-wp-manga-buy-coin .btn-agree').show();
					}
				}
				
				matches = cl.match(/data-chapter-\d+/g);
				if(matches){
					var chapter_id = matches[0].replace('data-chapter-', '');
					$('#frm-wp-manga-buy-coin input[name="wp-manga-chapter"]').val(chapter_id);
				}
				
				$('#frm-wp-manga-buy-coin').modal();
			}
		} else {
			window.wp_manga_chapter_coin_just_add_premium = false;
		}
		
		evt.stopPropagation();
		evt.preventDefault();
		return false;
	};
	
	
	$(document).on('wp_manga_after_load_chapters_list wp_manga_chapterNavigationAjax_done wp_manga_after_paginated', function(){
		$('.premium-block a').unbind('click', wp_manga_premium_block_click);
		$('.premium-block a').on('click', wp_manga_premium_block_click);
	});
	
	$('#frm-wp-manga-buy-coin .btn-cancel').on('click', function(){
		$('#frm-wp-manga-buy-coin').modal('hide');
		
		return false;
	});
	
	$('#frm-wp-manga-buy-coin .btn-agree').on('click', function(){
		
		if(!$(this).hasClass('disabled')){
			var nonce = $('#frm-wp-manga-buy-coin input[name="wp-manga-coin-nonce"]').val();
			
			var chapter_id = $('#frm-wp-manga-buy-coin input[name="wp-manga-chapter"]').val();
			if(chapter_id){
				$(this).addClass('disabled');
				var clickedButton = $(this);
				$.ajax({
					url: manga.ajax_url,
					type: 'POST',
					data: {
						action: 'wp_manga_buy_chapter',
						chapter: chapter_id,
						nonce: nonce
					},
					success: function(data){
						data = data.data;
						if(data.status){
							window.location = data.url;
						} else {
							alert(data.message);
							clickedButton.removeClass('disabled');
						}
						
						$('#frm-wp-manga-buy-coin input[name="wp-manga-coin-nonce"]').val(data.nonce);
					},
					error: function(xhr, ajaxOptions, thrownError){
						alert(xhr.responseText);
						clickedButton.removeClass('disabled');
					}
				});
			}
		}
		
		return false;
	});
	
	/**
	 * Update next/prev button status for checking premium-block chapter
	 **/
	$(document).on('wp_manga_after_load_chapter_page', function(evt, page){
		var current_chapter = $('.single-chapter-select option:selected');
		if(page == 1 || page == $('.entry-header.header #single-pager option').length){
			// check for next/prev premium chapter class
			if(page == 1){
				var prev_chapter = current_chapter.prev();
				if(typeof prev_chapter !== 'undefined' && prev_chapter.data('redirect') == '#'){
					$('.nav-previous').addClass('premium-block').addClass('premium');
					var matches = prev_chapter.attr('class').match(/coin-\d+/g);
					if(matches){
						$('.nav-previous').addClass(matches[0]);
					}
					
					matches = prev_chapter.attr('class').match(/data-chapter-\d+/g);
					if(matches){
						$('.nav-previous').addClass(matches[0]);
					}
					
					window.wp_manga_chapter_coin_just_add_premium = true;
				}
			} else {
				var next_chapter = current_chapter.next();
				if(typeof next_chapter !== 'undefined' && next_chapter.data('redirect') == '#'){
					$('.nav-next').addClass('premium-block').addClass('premium');
					var matches = next_chapter.attr('class').match(/coin-\d+/g);
					if(matches){
						$('.nav-next').addClass(matches[0]);
					}
					
					matches = next_chapter.attr('class').match(/data-chapter-\d+/g);
					if(matches){
						$('.nav-next').addClass(matches[0]);
					}
					
					window.wp_manga_chapter_coin_just_add_premium = true;
				}
			}
		} else {
			// remove premium chapter class
			if(current_chapter.data('redirect') != '#'){
				$('.nav-next').removeClass('premium-block');
				$('.nav-previous').removeClass('premium-block');
				var matches = $('.nav-next').attr('class').match(/coin-\d+/g);
				if(matches){
					$('.nav-next').removeClass(matches[0]);
				}
				
				matches = $('.nav-next').attr('class').match(/data-chapter-\d+/g);
				if(matches){
					$('.nav-next').removeClass(matches[0]);
				}
				
				matches = $('.nav-previous').attr('class').match(/coin-\d+/g);
				if(matches){
					$('.nav-previous').removeClass(matches[0]);
				}
				
				matches = $('.nav-previous').attr('class').match(/data-chapter-\d+/g);
				if(matches){
					$('.nav-previous').removeClass(matches[0]);
				}
				
				window.wp_manga_chapter_coin_just_add_premium = false;
			}
		}
		
		// reattach event
		$('.premium-block a').unbind('click', wp_manga_premium_block_click);
		$('.premium-block a').on('click', wp_manga_premium_block_click);
	});
	
	$('#madara-user-upload-pro-form').on('muupro_before_upload_chapters', function(evt, formData){
		var newFormData = formData;
		
		newFormData.append('chapter-coin', $('#chapter-coin').val());
		
		window.muupro_editchapter = newFormData;
	});
	
	$(document).on('muupro_get_chapter_info', function(evt, data) {
		if(!data.allow_set_coin){
			$('#madara-chapter-coin-row').remove();
		} else {
			if(data.chapter_coin){
				$('#chapter-coin').val(data.chapter_coin);
			}
		}
	});
	
	$(document).on('muupro_before_save_chapter', function(evt, formData){
		if($('#chapter-coin').length > 0){
			var newFormData = formData;
			
			newFormData.append('chapter-coin',$('#chapter-coin').val());
			
			window.muupro_editchapter = newFormData;
			$('#madara-chapter-coin').val([]);
		}
	});

	//reload after login successfully
	$(document).on("manga-login-success", function(){
		location.reload();
	});
});