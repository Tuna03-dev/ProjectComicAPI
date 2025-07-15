(function ($) {

	'use strict';

	$(document).ready(function () {
		
		// support WP Manga Member Upload PRO
		var uploadForm = $('#madara-user-upload-pro-form');
		uploadForm.on('muupro_before_upload_chapters', function(evt, formData) {
			var newFormData = formData;
			
			// add chapter thumbnail to the formdata
			if($('#upload-chapter-thumbnail').val()){
				newFormData.append('chapter-thumbnail', $('#upload-chapter-thumbnail')[0].files[0]);
			}
			
			window.muupro_uploadchapters = newFormData;
		});
		
		$(document).on('muupro_before_save_chapter', function(evt, formData){
			var newFormData = formData;
			
			// add chapter thumbnail to the formdata
			if($('#edit-madara-chapter-thumbnail').val()){
				newFormData.append('chapter-thumbnail', $('#edit-madara-chapter-thumbnail')[0].files[0]);
			}
			
			window.muupro_editchapter = newFormData;
			$('#edit-madara-chapter-thumbnail').val('');
		});
		
		$(document).on('muupro_get_chapter_info', function(evt, data) {
			if(data.chapter_thumbnail){
				$('#current-chapter-thumbnail').attr('src', data.chapter_thumbnail);
			}
		});
	});

})(jQuery);
