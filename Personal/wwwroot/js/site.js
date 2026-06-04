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

  document.querySelectorAll(".top-menu__group").forEach((group) => {
    const trigger = group.querySelector(".top-menu__item");
    if (!trigger) {
      return;
    }

    group.addEventListener("mouseenter", () => {
      trigger.setAttribute("aria-expanded", "true");
    });

    group.addEventListener("mouseleave", () => {
      trigger.setAttribute("aria-expanded", "false");
    });

    group.addEventListener("focusin", () => {
      trigger.setAttribute("aria-expanded", "true");
    });

    group.addEventListener("focusout", (event) => {
      if (!group.contains(event.relatedTarget)) {
        trigger.setAttribute("aria-expanded", "false");
      }
    });
  });

  const adminModal = document.querySelector("[data-admin-modal]");
  const adminModalOpenControls = document.querySelectorAll("[data-admin-modal-open]");
  const adminModalCloseControls = document.querySelectorAll("[data-admin-modal-close]");
  const adminPasswordForm = document.querySelector("[data-admin-password-form]");
  const adminPasswordInput = document.querySelector("[data-admin-password-input]");
  const adminPasswordError = document.querySelector("[data-admin-password-error]");

  const closeAdminModal = () => {
    if (!adminModal) {
      return;
    }

    adminModal.hidden = true;
    document.body.classList.remove("is-modal-open");

    if (adminPasswordInput) {
      adminPasswordInput.value = "";
    }

    if (adminPasswordError) {
      adminPasswordError.hidden = true;
    }
  };

  if (adminModal && adminModalOpenControls.length > 0) {
    adminModalOpenControls.forEach((control) => control.addEventListener("click", () => {
      adminModal.hidden = false;
      document.body.classList.add("is-modal-open");
      requestAnimationFrame(() => adminPasswordInput?.focus());
    }));
  }

  adminModalCloseControls.forEach((control) => {
    control.addEventListener("click", closeAdminModal);
  });

  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape" && adminModal && !adminModal.hidden) {
      closeAdminModal();
    }
  });

  if (adminPasswordForm && adminPasswordInput) {
    adminPasswordForm.addEventListener("submit", (event) => {
      event.preventDefault();

      if (adminPasswordInput.value === "qwerty") {
        window.location.href = "/Home/Admin";
        return;
      }

      if (adminPasswordError) {
        adminPasswordError.hidden = false;
      }

      adminPasswordInput.select();
    });
  }

  const adminShell = document.querySelector(".admin-shell");
  const adminScrollKey = "nzo-admin-scroll-y";
  const saveAdminScrollPosition = () => {
    sessionStorage.setItem(adminScrollKey, String(window.scrollY));
  };

  if (adminShell) {
    if ("scrollRestoration" in history) {
      history.scrollRestoration = "manual";
    }

    const savedScrollPosition = Number(sessionStorage.getItem(adminScrollKey));
    if (Number.isFinite(savedScrollPosition) && savedScrollPosition > 0) {
      sessionStorage.removeItem(adminScrollKey);
      requestAnimationFrame(() => {
        window.scrollTo({ top: savedScrollPosition, behavior: "auto" });
      });
    }

    adminShell.querySelectorAll("form").forEach((form) => {
      form.addEventListener("submit", saveAdminScrollPosition);
    });

    adminShell.querySelectorAll(".finder-sidebar__item, .finder-sidebar__subitem").forEach((link) => {
      link.addEventListener("click", saveAdminScrollPosition);
    });
  }

  document.addEventListener("submit", (event) => {
    const form = event.target.closest("form[data-confirm]");
    if (form && !window.confirm(form.dataset.confirm)) {
      event.preventDefault();
    }
  });

  document.querySelectorAll("[data-series-gallery]").forEach((gallery) => {
    const grid = gallery.querySelector("[data-series-grid]");
    const sourceCards = [...gallery.querySelectorAll("[data-series-card]")];
    const viewButtons = [...gallery.querySelectorAll("[data-series-view]")];
    const previousButton = gallery.querySelector("[data-series-prev]");
    const nextButton = gallery.querySelector("[data-series-next]");
    const status = gallery.querySelector("[data-series-status]");
    const pageSize = Math.max(1, Number(gallery.dataset.seriesPageSize) || 5);
    const rowSize = Math.max(1, Math.floor(pageSize / 2));
    const landscapeCards = sourceCards.filter((card) => card.dataset.seriesOrientation === "landscape");
    const portraitCards = sourceCards.filter((card) => card.dataset.seriesOrientation === "portrait");
    const otherCards = sourceCards.filter((card) => !["landscape", "portrait"].includes(card.dataset.seriesOrientation));
    const cards = [];
    let currentPage = 0;

    for (let index = 0; index < Math.max(landscapeCards.length, portraitCards.length); index += rowSize) {
      cards.push(...landscapeCards.slice(index, index + rowSize));
      cards.push(...portraitCards.slice(index, index + rowSize));
    }

    cards.push(...otherCards);

    if (grid) {
      cards.forEach((card) => grid.append(card));
    }

    const renderSeriesPage = () => {
      const totalPages = Math.max(1, Math.ceil(cards.length / pageSize));
      currentPage = Math.min(Math.max(currentPage, 0), totalPages - 1);
      const start = currentPage * pageSize;
      const end = Math.min(start + pageSize, cards.length);

      cards.forEach((card, index) => {
        const isVisible = index >= start && index < end;
        card.hidden = !isVisible;

        if (isVisible) {
          card.classList.add("active");
        }
      });

      if (previousButton) {
        previousButton.disabled = currentPage === 0;
      }

      if (nextButton) {
        nextButton.disabled = currentPage >= totalPages - 1;
      }

      if (status) {
        status.textContent = cards.length === 0 ? "0 of 0" : `${start + 1}-${end} of ${cards.length}`;
      }
    };

    viewButtons.forEach((button) => {
      button.addEventListener("click", () => {
        const view = button.dataset.seriesView;
        if (!grid || !view) {
          return;
        }

        grid.classList.toggle("series-grid--grid", view === "grid");
        grid.classList.toggle("series-grid--masonry", view === "masonry");

        viewButtons.forEach((viewButton) => {
          const isActive = viewButton === button;
          viewButton.classList.toggle("series-view-toggle__button--active", isActive);
          viewButton.setAttribute("aria-pressed", String(isActive));
        });
      });
    });

    if (previousButton) {
      previousButton.addEventListener("click", () => {
        currentPage -= 1;
        renderSeriesPage();
      });
    }

    if (nextButton) {
      nextButton.addEventListener("click", () => {
        currentPage += 1;
        renderSeriesPage();
      });
    }

    renderSeriesPage();
  });

  document.querySelectorAll(".featured-series-admin").forEach((seriesAdmin) => {
    if (seriesAdmin.matches("[data-admin-series-finder]")) {
      return;
    }

    const cards = [...seriesAdmin.querySelectorAll(":scope > .featured-series-admin__card")];
    if (cards.length === 0) {
      return;
    }

    const list = document.createElement("aside");
    list.className = "featured-series-finder__list";
    list.setAttribute("aria-label", "Featured series");

    const detail = document.createElement("div");
    detail.className = "featured-series-finder__detail";

    cards.forEach((card, index) => {
      const seriesId = card.querySelector('input[name="seriesId"]')?.value || `series-${index}`;
      const title = card.querySelector('input[name="title"]')?.value || `Series ${index + 1}`;
      const eyebrow = card.querySelector('input[name="eyebrow"]')?.value || `Series ${index + 1}`;
      const orientation = card.querySelector('select[name="orientation"]')?.value || "portrait";
      const coverImage = card.querySelector(".featured-series-admin__cover img")?.getAttribute("src") || "";
      const photoCount = card.querySelectorAll(".featured-series-admin__photos figure").length;
      const isSelected = index === 0;
      const button = document.createElement("button");
      const image = document.createElement("img");
      const text = document.createElement("span");
      const strong = document.createElement("strong");
      const small = document.createElement("small");

      button.className = `featured-series-finder__item${isSelected ? " featured-series-finder__item--active" : ""}`;
      button.type = "button";
      button.dataset.adminSeriesTab = seriesId;
      button.setAttribute("aria-pressed", String(isSelected));
      image.src = coverImage;
      image.alt = "";
      strong.textContent = title;
      small.textContent = `${eyebrow} · ${orientation} · ${photoCount} photo${photoCount === 1 ? "" : "s"}`;
      text.append(strong, small);
      button.append(image, text);
      list.append(button);

      const coverForm = card.querySelector('.featured-series-admin__tools form[action*="SetFeaturedSeriesCover"]');
      if (coverForm) {
        const coverPanel = document.createElement("div");
        const coverButton = document.createElement("button");

        coverPanel.className = "featured-series-admin__tool-panel";
        coverPanel.innerHTML = "<span>Cover image</span>";
        coverButton.type = "button";
        coverButton.dataset.seriesCoverModalOpen = seriesId;
        coverButton.textContent = "Choose cover";
        coverPanel.append(coverButton);
        coverForm.replaceWith(coverPanel);
      }

      if (!card.querySelector(".featured-series-admin__remove-series")) {
        const tools = card.querySelector(".featured-series-admin__tools");
        const referenceForm = card.querySelector(".featured-series-admin__edit");
        const token = referenceForm?.querySelector('input[name="__RequestVerificationToken"]')?.cloneNode();
        const removeSeriesForm = document.createElement("form");
        const seriesInput = document.createElement("input");
        const librarySourceInput = card.querySelector('input[name="librarySource"]')?.cloneNode();
        const cloudinaryFolderInput = card.querySelector('input[name="cloudinaryFolder"]')?.cloneNode();
        const removeButton = document.createElement("button");

        removeSeriesForm.className = "featured-series-admin__remove-series";
        removeSeriesForm.method = "post";
        removeSeriesForm.action = "/Home/RemoveFeaturedSeries";
        removeSeriesForm.dataset.confirm = "Remove this entire featured series?";
        seriesInput.type = "hidden";
        seriesInput.name = "seriesId";
        seriesInput.value = seriesId;
        removeButton.type = "submit";
        removeButton.textContent = "Remove series";
        [token, seriesInput, librarySourceInput, cloudinaryFolderInput, removeButton]
          .filter(Boolean)
          .forEach((node) => removeSeriesForm.append(node));
        tools?.append(removeSeriesForm);
      }

      if (!card.querySelector("[data-series-cover-modal]")) {
        const modal = document.createElement("div");
        const dialog = document.createElement("section");
        const closeButton = document.createElement("button");
        const label = document.createElement("span");
        const heading = document.createElement("h3");
        const grid = document.createElement("div");

        modal.className = "series-cover-modal";
        modal.dataset.seriesCoverModal = seriesId;
        modal.hidden = true;
        modal.innerHTML = '<div class="series-cover-modal__backdrop" data-series-cover-modal-close></div>';
        dialog.className = "series-cover-modal__dialog";
        dialog.setAttribute("role", "dialog");
        dialog.setAttribute("aria-modal", "true");
        closeButton.className = "series-cover-modal__close";
        closeButton.type = "button";
        closeButton.setAttribute("aria-label", "Close cover picker");
        closeButton.dataset.seriesCoverModalClose = "";
        closeButton.innerHTML = '<span class="material-symbols-outlined" aria-hidden="true">close</span>';
        label.className = "label";
        label.textContent = "Cover Image";
        heading.textContent = title;
        grid.className = "series-cover-modal__grid";

        card.querySelectorAll(".featured-series-admin__photos figure img").forEach((photo) => {
          const photoUrl = photo.getAttribute("src");
          const referenceForm = card.querySelector(".featured-series-admin__edit");
          const coverToken = referenceForm?.querySelector('input[name="__RequestVerificationToken"]')?.cloneNode();
          const removeToken = referenceForm?.querySelector('input[name="__RequestVerificationToken"]')?.cloneNode();
          const item = document.createElement("div");
          const coverForm = document.createElement("form");
          const removeForm = document.createElement("form");
          const seriesInput = document.createElement("input");
          const imageInput = document.createElement("input");
          const removeSeriesInput = document.createElement("input");
          const removeImageInput = document.createElement("input");
          const librarySourceInput = card.querySelector('input[name="librarySource"]')?.cloneNode();
          const cloudinaryFolderInput = card.querySelector('input[name="cloudinaryFolder"]')?.cloneNode();
          const removeLibrarySourceInput = card.querySelector('input[name="librarySource"]')?.cloneNode();
          const removeCloudinaryFolderInput = card.querySelector('input[name="cloudinaryFolder"]')?.cloneNode();
          const choiceButton = document.createElement("button");
          const choiceImage = document.createElement("img");
          const choiceLabel = document.createElement("span");
          const removeButton = document.createElement("button");
          const isCover = photoUrl === coverImage;

          item.className = "series-cover-modal__item";
          coverForm.method = "post";
          coverForm.action = "/Home/SetFeaturedSeriesCover";
          seriesInput.type = "hidden";
          seriesInput.name = "seriesId";
          seriesInput.value = seriesId;
          imageInput.type = "hidden";
          imageInput.name = "imageUrl";
          imageInput.value = photoUrl;
          choiceButton.className = `series-cover-modal__choice${isCover ? " series-cover-modal__choice--active" : ""}`;
          choiceButton.type = "submit";
          choiceImage.src = photoUrl;
          choiceImage.alt = `${title} cover option`;
          choiceLabel.textContent = isCover ? "Current cover" : "Set cover";
          choiceButton.append(choiceImage, choiceLabel);
          [coverToken, seriesInput, imageInput, librarySourceInput, cloudinaryFolderInput, choiceButton]
            .filter(Boolean)
            .forEach((node) => coverForm.append(node));
          removeForm.method = "post";
          removeForm.action = "/Home/RemoveFeaturedSeriesPhoto";
          removeForm.dataset.confirm = "Remove this photo from the series?";
          removeSeriesInput.type = "hidden";
          removeSeriesInput.name = "seriesId";
          removeSeriesInput.value = seriesId;
          removeImageInput.type = "hidden";
          removeImageInput.name = "imageUrl";
          removeImageInput.value = photoUrl;
          removeButton.className = "series-cover-modal__remove";
          removeButton.type = "submit";
          removeButton.textContent = "Remove photo";
          [removeToken, removeSeriesInput, removeImageInput, removeLibrarySourceInput, removeCloudinaryFolderInput, removeButton]
            .filter(Boolean)
            .forEach((node) => removeForm.append(node));
          item.append(coverForm, removeForm);
          grid.append(item);
        });

        dialog.append(closeButton, label, heading, grid);
        modal.append(dialog);
        card.append(modal);
      }

      card.dataset.adminSeriesPanel = seriesId;
      card.hidden = !isSelected;
      detail.append(card);
    });

    seriesAdmin.classList.add("featured-series-finder");
    seriesAdmin.dataset.adminSeriesFinder = "";
    seriesAdmin.replaceChildren(list, detail);
  });

  document.querySelectorAll("[data-admin-series-finder]").forEach((finder) => {
    const tabs = [...finder.querySelectorAll("[data-admin-series-tab]")];
    const panels = [...finder.querySelectorAll("[data-admin-series-panel]")];

    const showSeriesPanel = (seriesId) => {
      tabs.forEach((tab) => {
        const isActive = tab.dataset.adminSeriesTab === seriesId;
        tab.classList.toggle("featured-series-finder__item--active", isActive);
        tab.setAttribute("aria-pressed", String(isActive));
      });

      panels.forEach((panel) => {
        panel.hidden = panel.dataset.adminSeriesPanel !== seriesId;
      });
    };

    tabs.forEach((tab) => {
      tab.addEventListener("click", () => {
        showSeriesPanel(tab.dataset.adminSeriesTab);
      });
    });

    const activeTab = tabs.find((tab) => tab.classList.contains("featured-series-finder__item--active")) ?? tabs[0];
    if (activeTab) {
      showSeriesPanel(activeTab.dataset.adminSeriesTab);
    }
  });

  document.querySelectorAll("[data-series-cover-modal-open]").forEach((control) => {
    const modal = document.querySelector(`[data-series-cover-modal="${control.dataset.seriesCoverModalOpen}"]`);
    if (!modal) {
      return;
    }

    control.addEventListener("click", () => {
      modal.hidden = false;
      document.body.classList.add("is-modal-open");
    });
  });

  document.querySelectorAll("[data-series-cover-modal]").forEach((modal) => {
    modal.querySelectorAll("[data-series-cover-modal-close]").forEach((control) => {
      control.addEventListener("click", () => {
        modal.hidden = true;
        document.body.classList.remove("is-modal-open");
      });
    });
  });

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
