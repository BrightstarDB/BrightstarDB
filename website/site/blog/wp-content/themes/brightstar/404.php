<?php get_header(); ?>

<article class="post">
  <header>
    <h1>
      <?php _e('404 - Page not found','html5reset'); ?>
    </h1>
  </header>
  <h2>Recent Posts</h2>
  <ul class="archive">
    <?php
	      $recent_posts = wp_get_recent_posts();
	      foreach( $recent_posts as $recent ){
		      echo '<li><a href="' . get_permalink($recent["ID"]) . '" title="Read '.$recent["post_title"].'" >' .   $recent["post_title"].'</a> </li> ';
	      }
    ?>
  </ul>

</article>
<?php get_sidebar(); ?>

<?php get_footer(); ?>