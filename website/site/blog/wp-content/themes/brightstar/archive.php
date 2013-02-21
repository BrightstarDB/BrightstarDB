<?php get_header(); ?>

      <article class="archive">
		<?php if (have_posts()) : ?>

 			<?php $post = $posts[0]; // Hack. Set $post so that the_date() works. ?>

			<?php /* If this is a category archive */ if (is_category()) { ?>
			      <header><h1>Archive for the &#8216;<?php single_cat_title(); ?>&#8217; Category</h1></header>

			<?php /* If this is a tag archive */ } elseif( is_tag() ) { ?>
				<header><h1>Posts Tagged &#8216;<?php single_tag_title(); ?>&#8217;</h1></header>

			<?php /* If this is a daily archive */ } elseif (is_day()) { ?>
				<header><h1>Archive for <?php the_time('F jS, Y'); ?></h1></header>

			<?php /* If this is a monthly archive */ } elseif (is_month()) { ?>
				<header><h1>Archive for <?php the_time('F, Y'); ?></h1></header>

			<?php /* If this is a yearly archive */ } elseif (is_year()) { ?>
				<header><h1>Archive for <?php the_time('Y'); ?></h1></header>

			<?php /* If this is an author archive */ } elseif (is_author()) { ?>
				<header><h1>Author Archive</h1></header>

			<?php /* If this is a paged archive */ } elseif (isset($_GET['paged']) && !empty($_GET['paged'])) { ?>
				<h2 class="pagetitle">Blog Archives</h2>
			
			<?php } ?>
			
			<?php include (TEMPLATEPATH . '/_/inc/nav.php' ); ?>
      
      <ul class="archive">
			
      <?php while (have_posts()) : the_post(); ?>

        <li>
				    
						<a href="<?php the_permalink() ?>"><?php the_title(); ?></a>
          <time datetime="<?php echo date(DATE_W3C); ?>" pubdate class="updated"><?php the_time('F jS, Y') ?></time>

				</li>

			<?php endwhile; ?>
      
      </ul>

        <?php get_sidebar() ?>
        
        
			<?php include (TEMPLATEPATH . '/_/inc/nav.php' ); ?>
			
	<?php else : ?>

		<h2>Nothing found</h2>

	<?php endif; ?>

</article>

<?php get_footer(); ?>
