import { Directive, Input, ElementRef, inject, OnDestroy, AfterViewInit } from '@angular/core';

@Directive({
  selector: '[appLazyLoad]',
  standalone: true
})
export class LazyLoadDirective implements AfterViewInit, OnDestroy {
  @Input() appLazyLoad: string = ''; // Image source URL
  @Input() lazyLoadPlaceholder: string = 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMzAwIiBoZWlnaHQ9IjIwMCIgdmlld0JveD0iMCAwIDMwMCAyMDAiIGZpbGw9Im5vbmUiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyI+CjxyZWN0IHdpZHRoPSIzMDAiIGhlaWdodD0iMjAwIiBmaWxsPSIjRjNGNEY2Ii8+CjxwYXRoIGQ9Ik0xMzAgMTAwSDEzNVY5NUgxNDBWMTAwSDE0NVY5NUgxNTBWMTAwSDE1NVY5NUgxNjBWMTAwSDE2NVYxMDVIMTYwVjExMEgxNjVWMTE1SDE2MFYxMjBIMTY1VjEyNUgxNjBWMTMwSDE1NVYxMjVIMTUwVjEzMEgxNDVWMTI1SDE0MFYxMzBIMTM1VjEyNUgxMzBWMTIwSDEzNVYxMTVIMTMwVjExMEgxMzVWMTA1SDEzMFYxMDBaIiBmaWxsPSIjRDFEM0Q3Ii8+Cjwvc3ZnPgo='; // Placeholder image
  @Input() lazyLoadErrorSrc: string = ''; // Error fallback image
  @Input() lazyLoadRootMargin: string = '50px'; // Intersection observer root margin

  private readonly elementRef = inject(ElementRef);
  private intersectionObserver?: IntersectionObserver;

  ngAfterViewInit(): void {
    this.setupLazyLoading();
  }

  ngOnDestroy(): void {
    if (this.intersectionObserver) {
      this.intersectionObserver.disconnect();
    }
  }

  private setupLazyLoading(): void {
    const element = this.elementRef.nativeElement;

    // Set placeholder image initially
    if (element.tagName === 'IMG') {
      element.src = this.lazyLoadPlaceholder;
      element.style.filter = 'blur(5px)';
      element.style.transition = 'filter 0.3s';
    }

    // Create intersection observer
    this.intersectionObserver = new IntersectionObserver(
      (entries) => {
        entries.forEach(entry => {
          if (entry.isIntersecting) {
            this.loadImage();
            this.intersectionObserver?.unobserve(element);
          }
        });
      },
      {
        rootMargin: this.lazyLoadRootMargin,
        threshold: 0.1
      }
    );

    this.intersectionObserver.observe(element);
  }

  private loadImage(): void {
    const element = this.elementRef.nativeElement;

    if (element.tagName === 'IMG') {
      this.loadImageElement(element);
    } else {
      this.loadBackgroundImage(element);
    }
  }

  private loadImageElement(imgElement: HTMLImageElement): void {
    const tempImage = new Image();

    tempImage.onload = () => {
      imgElement.src = this.appLazyLoad;
      imgElement.style.filter = 'none';
      imgElement.classList.add('lazy-loaded');
    };

    tempImage.onerror = () => {
      if (this.lazyLoadErrorSrc) {
        imgElement.src = this.lazyLoadErrorSrc;
      }
      imgElement.style.filter = 'none';
      imgElement.classList.add('lazy-error');
    };

    tempImage.src = this.appLazyLoad;
  }

  private loadBackgroundImage(element: HTMLElement): void {
    const tempImage = new Image();

    tempImage.onload = () => {
      element.style.backgroundImage = `url(${this.appLazyLoad})`;
      element.style.filter = 'none';
      element.classList.add('lazy-loaded');
    };

    tempImage.onerror = () => {
      if (this.lazyLoadErrorSrc) {
        element.style.backgroundImage = `url(${this.lazyLoadErrorSrc})`;
      }
      element.style.filter = 'none';
      element.classList.add('lazy-error');
    };

    tempImage.src = this.appLazyLoad;
  }
}