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

  const uploadInput = document.querySelector("[data-upload-input]");
  const uploadLabel = document.querySelector("[data-upload-label]");
  const uploadPreview = document.querySelector("[data-upload-preview]");
  const uploadPreviewGrid = document.querySelector("[data-upload-preview-grid]");
  const uploadClear = document.querySelector("[data-upload-clear]");
  let selectedUploadFiles = [];
  let uploadPreviewUrls = [];

  const clearUploadPreviewUrls = () => {
    uploadPreviewUrls.forEach((url) => URL.revokeObjectURL(url));
    uploadPreviewUrls = [];
  };

  const formatFileSize = (size) => {
    if (size < 1024 * 1024) {
      return `${Math.max(1, Math.round(size / 1024))} KB`;
    }

    return `${(size / (1024 * 1024)).toFixed(1)} MB`;
  };

  const resetUploadPreview = () => {
    clearUploadPreviewUrls();
    selectedUploadFiles = [];

    if (uploadPreviewGrid) {
      uploadPreviewGrid.replaceChildren();
    }

    if (uploadPreview) {
      uploadPreview.hidden = true;
    }

    if (uploadLabel) {
      uploadLabel.textContent = "Add images";
    }
  };

  const syncUploadInputFiles = () => {
    if (!uploadInput || typeof DataTransfer === "undefined") {
      return;
    }

    const dataTransfer = new DataTransfer();
    selectedUploadFiles.forEach((file) => dataTransfer.items.add(file));
    uploadInput.files = dataTransfer.files;
  };

  const renderUploadPreview = () => {
    clearUploadPreviewUrls();

    if (uploadPreviewGrid) {
      uploadPreviewGrid.replaceChildren();
    }

    if (selectedUploadFiles.length === 0) {
      if (uploadPreview) {
        uploadPreview.hidden = true;
      }

      if (uploadLabel) {
        uploadLabel.textContent = "Add images";
      }

      syncUploadInputFiles();
      return;
    }

    if (uploadLabel) {
      uploadLabel.textContent = `${selectedUploadFiles.length} image${selectedUploadFiles.length === 1 ? "" : "s"} selected`;
    }

    selectedUploadFiles.forEach((file, index) => {
      const imageUrl = URL.createObjectURL(file);
      uploadPreviewUrls.push(imageUrl);

      const card = document.createElement("article");
      card.className = "upload-preview-card";

      const image = document.createElement("img");
      image.src = imageUrl;
      image.alt = file.name;

      const body = document.createElement("div");
      body.className = "upload-preview-card__body";

      const name = document.createElement("span");
      name.textContent = file.name;

      const meta = document.createElement("strong");
      meta.textContent = index === 0 ? `${formatFileSize(file.size)} | first` : formatFileSize(file.size);

      const removeButton = document.createElement("button");
      removeButton.type = "button";
      removeButton.textContent = "Remove";
      removeButton.addEventListener("click", () => {
        selectedUploadFiles.splice(index, 1);
        renderUploadPreview();
      });

      body.append(name, meta, removeButton);
      card.append(image, body);
      uploadPreviewGrid.append(card);
    });

    if (uploadPreview) {
      uploadPreview.hidden = false;
    }

    syncUploadInputFiles();
  };

  if (uploadInput && uploadPreview && uploadPreviewGrid) {
    uploadInput.addEventListener("change", () => {
      const incomingFiles = [...uploadInput.files].filter((file) => file.type.startsWith("image/"));
      const existingFileKeys = new Set(selectedUploadFiles.map((file) => `${file.name}:${file.size}:${file.lastModified}`));
      const newFiles = incomingFiles.filter((file) => !existingFileKeys.has(`${file.name}:${file.size}:${file.lastModified}`));
      selectedUploadFiles = selectedUploadFiles.concat(newFiles);
      renderUploadPreview();
    });
  }

  if (uploadInput && uploadClear) {
    uploadClear.addEventListener("click", () => {
      uploadInput.value = "";
      resetUploadPreview();
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
