<footer class="postmetadata">
  <!--
	Posted by <span class="byline author vcard"><span class="fn"><?php the_author() ?></span></span> in <?php the_category(', ') ?>
  -->
  <?php comments_popup_link('No Comments', '1 Comment', '% Comments', 'comments-link', ''); ?>.&nbsp;
  <?php the_tags('Tags: ', ', ', '<br />'); ?>
	
</footer>