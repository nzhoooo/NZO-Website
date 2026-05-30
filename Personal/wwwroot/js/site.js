document.addEventListener("DOMContentLoaded", () => {
  const heroSlides = [...document.querySelectorAll(".hero__slide")];
  const topNav = document.getElementById("top-nav");
  const themeToggle = document.getElementById("theme-toggle");
  const motionQuery = window.matchMedia("(prefers-reduced-motion: reduce)");
  let activeHeroSlide = 0;
  const dockLinks = [...document.querySelectorAll(".dock__item")];
  const dockTargets = dockLinks
    .map((link) => ({
      link,
      target: document.querySelector(link.hash)
    }))
    .filter((item) => item.target);

  const updateTopNav = () => {
    if (!topNav) {
      return;
    }

    topNav.classList.toggle("is-scrolled", window.scrollY > 50);
  };

  const updateParallax = () => {
    if (heroSlides.length === 0 || motionQuery.matches) {
      return;
    }

    heroSlides.forEach((slide) => {
      slide.style.transform = `translateY(${window.scrollY * 0.4}px)`;
    });
  };

  const showHeroSlide = (index) => {
    heroSlides.forEach((slide, slideIndex) => {
      slide.classList.toggle("hero__slide--active", slideIndex === index);
    });
  };

  const updateDockState = () => {
    if (dockTargets.length === 0) {
      return;
    }

    const activeItem = dockTargets.reduce((current, item) => {
      const distance = Math.abs(item.target.getBoundingClientRect().top);
      return distance < current.distance ? { item, distance } : current;
    }, { item: dockTargets[0], distance: Number.POSITIVE_INFINITY }).item;

    dockLinks.forEach((link) => link.classList.toggle("dock__item--active", link === activeItem.link));
  };

  window.addEventListener("scroll", () => {
    updateTopNav();
    updateParallax();
    updateDockState();
  }, { passive: true });

  updateTopNav();
  updateParallax();
  updateDockState();

  if (heroSlides.length > 1 && !motionQuery.matches) {
    window.setInterval(() => {
      activeHeroSlide = (activeHeroSlide + 1) % heroSlides.length;
      showHeroSlide(activeHeroSlide);
    }, 5000);
  }

  if (themeToggle) {
    themeToggle.addEventListener("click", () => {
      document.body.classList.toggle("theme-dark");
    });
  }

  const revealItems = document.querySelectorAll(".reveal");
  if ("IntersectionObserver" in window && !motionQuery.matches) {
    const revealObserver = new IntersectionObserver((entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          entry.target.classList.add("active");
          revealObserver.unobserve(entry.target);
        }
      });
    }, {
      threshold: 0.15,
      rootMargin: "0px 0px -50px 0px"
    });

    revealItems.forEach((item) => revealObserver.observe(item));
  } else {
    revealItems.forEach((item) => item.classList.add("active"));
  }

  if (!motionQuery.matches) {
    document.querySelectorAll(".tilt-card").forEach((card) => {
      card.addEventListener("mousemove", (event) => {
        const rect = card.getBoundingClientRect();
        const x = event.clientX - rect.left;
        const y = event.clientY - rect.top;
        const centerX = rect.width / 2;
        const centerY = rect.height / 2;
        const rotateX = (y - centerY) / 25;
        const rotateY = (centerX - x) / 25;

        card.style.transform = `perspective(1000px) rotateX(${rotateX}deg) rotateY(${rotateY}deg) scale3d(1.02, 1.02, 1.02)`;
        card.style.setProperty("--x", `${(x / rect.width) * 100}%`);
        card.style.setProperty("--y", `${(y / rect.height) * 100}%`);
      });

      card.addEventListener("mouseleave", () => {
        card.style.transform = "perspective(1000px) rotateX(0deg) rotateY(0deg) scale3d(1, 1, 1)";
      });
    });
  }
});
