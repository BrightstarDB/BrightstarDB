.. _BrightstarDB Security:

######################
BrightstarDB Security
######################

This section covers the topic of BrightstarDB server security from multiple viewpoints.
The security features of BrightstarDB are basic but designed to be customizable to fit
with different schemes of user authentication and authorization.


*****************
Access Control
*****************

.. note::

    Access controls for BrightstarDB services is a work in progress. Previous releases had
    no form of access control and there is much work to complete to reach the desired state.
    Rather than wait until everything is all completed, this release provides the framework
    for access controls and future releases will build on that framework to deliver incremental
    increases in functionality. Comments and suggestions for improvements in this area
    are most welcome.

.. _Store Permissions:

Store Permissions
=================

BrightstarDB is secured at the store level. A user that has read access to a store
has read access to all the data in the store. A user with the required update privileges
can update or delete any of the triples in the store. The permissions for a user on a store
can be any combination of the following:

    None
        The user has no permissions on the store and can perform no operations on it at all
    
    Read
        The user has permission to perform SPARQL queries on the store
        
    Export
        The user can run an export job to retrieve a dump of the RDF contained in the store
        
    ViewHistory
        The user can view the commit and transaction history of the store
        
    SparqlUpdate
        The user can post updates to the store using the SPARQL update protocol
        
    TransactionUpdate
        The user can post updates to the store using the BrightstarDB transactional update protocol
        
    Admin
        The user can re-execute previous transactions; revert the store to a previous transaction;
        and delete the store
        
    WithGrant
        The user can grant permissions on this store to other users
        
    All
        A combination of all of the above permissions

.. _System Permissions:
        
System Permissions
==================

In addition to permissions on individual stores, users can also be assigned permissions on the
BrightstarDB server as a whole. These permissions control only the ability to list, create and
delete stores. The system permissions for a user can be any combination of the following:

    None
        The user has no system permissions. This level denies even the listing of the stores
        currently available on the server.
        
    ListStores
        The user can list the stores available on the server. Note that the listing is not
        currently filtered by store access permissions, so the user will see all stores
        regardless of whether or not they have any permission to access the stores.
        
    CreateStore
        The user can create new stores on the server.
        
    Admin
        The user can delete stores from the server regardless of whether they have permissions
        to administer the individual stores themselves.
        
    All
        A combination of all the above permissions.

.. Authentication:

*********************
Authentication
*********************

User authentication is the responsibility of the host application for the BrightstarDB
service. There are several different approaches which can be taken to user authentication
for a REST-based API and the structure of BrightstarDB enables you to plug or leverage
the form of authentication that works best for your solution.

Credential-based Authentication
===============================

If the BrightstarDB service is hosted under IIS, you can use IIS Basic Authentication or
Windows Authentication to protect the service. This requires that the client provides
credentials and that those credentials are checked for each request made. If the credentials
are valid, the user identity for the request will be set to the identity associated with
the credentials. If the credentials are invalid, the request will be rejected without
further processing.

Shared Secret Authentication
============================

An alternative to id/password credentials is to use a shared secret key mechanism. The
BrightstarDB server and client share a secret key which is used to sign requests. The 
key is provided to the client by some mechanism outside of the BrightstarDB service
itself (e.g. you might email the key or provide a separate web endpoint for requesting
and providing keys). Each secret key is associated with an account ID. The requestor
includes their account ID in the request and then signs the content of the request
using their secret key. The server checks for the account ID, and validates the
signature on the request using the same key. If the signature is valid, the 
identity for the request is set to the identity associated with the account ID.
If the signature is not valid, the request is rejected without further processing.

.. note::

    Currently this form of authentication is not yet implemented on the server.
    It is planned to add support for this in a future release and to provide
    a simple service for managing account/secret pairs in a BrightstarDB
    store so that it is easy to integrate key generation and management into
    an existing site.

.. _Authorization:

*********************
Authorization
*********************

BrightstarDB has an extensible solution for the task of determining the precise permissions of
a specific user. Permission Providers are classes that are responsible for returning the 
permission flags for a user. 

Store Permission Providers determine the permissions for a given user (or the anonymous user)
on a given store. System Permission Providers determine the permissions for a given user on the
BrightstarDB server.

Possible means of determining the permissions for a user include:

    Fixed Permission Levels
        All users have the same level of access to all stores. A variation of this specifies
        on set of permissions for authenticated users and another set of permissions for
        anonymous users.
        
    Statically Configured Permission Levels
        Users are assigned permissions from a master list of permissions. This master list
        might be kept in a file or in a BrightstarDB store. Either way the permissions list
        needs to be manually updated when new stores are created or users are added to or removed
        from the system.
        
        Alternatively permissions can be statically assigned to roles. Authenticated
        users are associated with one or more roles and receive permissions based on adding
        together all the permissions of all of their roles. This requires that the authentication
        system be capable of returning a set of roles for an authenticated user.
        
    Dynamically Configured Permission Levels
        Users or roles are assigned permissions from a master list of permissions kept in a
        BrightstarDB store. These permissions can be updated through the BrightstarDB 
        Admin API.
        
.. note::

    Currently only support for Fixed Permission Levels is implemented. Support for the other forms
    of authentication will be added in forthcoming releases.
        