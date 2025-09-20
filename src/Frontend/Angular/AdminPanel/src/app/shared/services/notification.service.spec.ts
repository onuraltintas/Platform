import { TestBed } from '@angular/core/testing';
import { NotificationService, NotificationType } from './notification.service';

describe('NotificationService', () => {
  let service: NotificationService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(NotificationService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('success notifications', () => {
    it('should add success notification', () => {
      const message = 'Operation successful';
      const title = 'Success';

      service.success(message, title);

      const notifications = service.getNotifications();
      expect(notifications).toHaveLength(1);
      expect(notifications[0]).toEqual(
        expect.objectContaining({
          type: 'success',
          message,
          title,
          duration: 5000
        })
      );
    });

    it('should add success notification without title', () => {
      const message = 'Operation successful';

      service.success(message);

      const notifications = service.getNotifications();
      expect(notifications[0].title).toBeUndefined();
    });
  });

  describe('error notifications', () => {
    it('should add error notification', () => {
      const message = 'Operation failed';
      const title = 'Error';

      service.error(message, title);

      const notifications = service.getNotifications();
      expect(notifications).toHaveLength(1);
      expect(notifications[0]).toEqual(
        expect.objectContaining({
          type: 'error',
          message,
          title,
          duration: 8000 // Error notifications have longer duration
        })
      );
    });
  });

  describe('warning notifications', () => {
    it('should add warning notification', () => {
      const message = 'Be careful';
      const title = 'Warning';

      service.warning(message, title);

      const notifications = service.getNotifications();
      expect(notifications).toHaveLength(1);
      expect(notifications[0]).toEqual(
        expect.objectContaining({
          type: 'warning',
          message,
          title,
          duration: 5000
        })
      );
    });
  });

  describe('info notifications', () => {
    it('should add info notification', () => {
      const message = 'Information message';
      const title = 'Info';

      service.info(message, title);

      const notifications = service.getNotifications();
      expect(notifications).toHaveLength(1);
      expect(notifications[0]).toEqual(
        expect.objectContaining({
          type: 'info',
          message,
          title,
          duration: 5000
        })
      );
    });
  });

  describe('multiple notifications', () => {
    it('should handle multiple notifications', () => {
      service.success('Success 1');
      service.error('Error 1');
      service.warning('Warning 1');

      const notifications = service.getNotifications();
      expect(notifications).toHaveLength(3);
      expect(notifications[0].type).toBe('success');
      expect(notifications[1].type).toBe('error');
      expect(notifications[2].type).toBe('warning');
    });
  });

  describe('dismiss notifications', () => {
    it('should dismiss notification by id', () => {
      service.success('Test message');
      const notifications = service.getNotifications();
      const notificationId = notifications[0].id;

      service.dismiss(notificationId);

      expect(service.getNotifications()).toHaveLength(0);
    });

    it('should dismiss all notifications', () => {
      service.success('Success message');
      service.error('Error message');
      service.info('Info message');

      expect(service.getNotifications()).toHaveLength(3);

      service.dismissAll();

      expect(service.getNotifications()).toHaveLength(0);
    });
  });

  describe('notification properties', () => {
    it('should generate unique ids for notifications', () => {
      service.success('Message 1');
      service.success('Message 2');

      const notifications = service.getNotifications();
      expect(notifications[0].id).not.toBe(notifications[1].id);
    });

    it('should set timestamp for notifications', () => {
      const beforeTime = new Date().getTime();
      service.success('Test message');
      const afterTime = new Date().getTime();

      const notification = service.getNotifications()[0];
      expect(notification.timestamp.getTime()).toBeGreaterThanOrEqual(beforeTime);
      expect(notification.timestamp.getTime()).toBeLessThanOrEqual(afterTime);
    });
  });

  describe('custom durations', () => {
    it('should respect custom duration for success notifications', () => {
      const customDuration = 10000;
      service.success('Test message', 'Title', customDuration);

      const notification = service.getNotifications()[0];
      expect(notification.duration).toBe(customDuration);
    });

    it('should respect custom duration for warning notifications', () => {
      const customDuration = 3000;
      service.warning('Test message', 'Title', customDuration);

      const notification = service.getNotifications()[0];
      expect(notification.duration).toBe(customDuration);
    });
  });

  describe('observable notifications', () => {
    it('should emit notifications through observable', (done) => {
      let notificationCount = 0;

      service.notifications$.subscribe(notifications => {
        notificationCount++;
        if (notificationCount === 1) {
          expect(notifications).toHaveLength(0); // Initial state
        } else if (notificationCount === 2) {
          expect(notifications).toHaveLength(1); // After adding one
          expect(notifications[0].message).toBe('Test message');
          done();
        }
      });

      service.success('Test message');
    });
  });

  describe('edge cases', () => {
    it('should handle empty messages', () => {
      service.success('');
      const notification = service.getNotifications()[0];
      expect(notification.message).toBe('');
    });

    it('should handle very long messages', () => {
      const longMessage = 'a'.repeat(1000);
      service.success(longMessage);
      const notification = service.getNotifications()[0];
      expect(notification.message).toBe(longMessage);
    });

    it('should handle special characters in messages', () => {
      const specialMessage = 'Test with <html> & "quotes" and emoji 🎉';
      service.success(specialMessage);
      const notification = service.getNotifications()[0];
      expect(notification.message).toBe(specialMessage);
    });
  });
});