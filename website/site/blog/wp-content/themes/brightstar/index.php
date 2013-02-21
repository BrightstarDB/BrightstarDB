<?php get_header(); ?>

	
	<?php $firstPost=true; ?>
	<?php if (have_posts()) : while (have_posts()) : the_post(); ?>

		<article <?php post_class() ?> id="post-<?php the_ID(); ?>">

			<header>
				 <time datetime="<?php echo date(DATE_W3C); ?>" pubdate class="updated"><?php the_time('F jS, Y') ?></time>
				<h1><a href="<?php the_permalink() ?>"><?php the_title(); ?></a></h1>
			</header>

			<section class="entry">
				<?php $firstPost ? the_content('Continue reading...') : the_excerpt(); ?>
			</section>

			<?php include (TEMPLATEPATH . '/_/inc/meta.php'); ?>

		</article>

		<?php if ($firstPost) :  ?>
		  <section class="previous-posts">
		  <header>
			<h1>Previous Entries</h1>
		  </header>
		  <?php $firstPost = false; ?>
		<?php endif; ?>
	<?php endwhile; ?>

    </section>

	<?php get_sidebar(); ?>

	<?php include (TEMPLATEPATH . '/_/inc/nav.php' ); ?>

	<?php else : ?>
		<article>
			<h2>Not Found</h2>
		</article>
	<?php endif; ?>

<?php get_footer(); ?>
